-- Run against a disposable database after schema creation and the RLS upgrade.
BEGIN;
INSERT INTO "AspNetUsers" ("Id", "DisplayName", "Kind", "EmailConfirmed", "PhoneNumberConfirmed", "TwoFactorEnabled", "LockoutEnabled", "AccessFailedCount") VALUES
    ('rls-a', 'A', 'Freelancer', true, false, false, true, 0),
    ('rls-b', 'B', 'Freelancer', true, false, false, true, 0),
    ('rls-c', 'C', 'Client', true, false, false, true, 0);
INSERT INTO "Clients" ("Id", "OwnerId", "UserId", "Name", "Company", "Email", "Phone", "Notes") VALUES
    ('00000000-0000-0000-0000-000000000001', 'rls-a', 'rls-c', 'C', '', 'c@example.com', '', ''),
    ('00000000-0000-0000-0000-000000000002', 'rls-b', NULL, 'D', '', 'd@example.com', '', '');
INSERT INTO "BusinessProfiles" ("OwnerId", "BusinessName", "ResponsibleName", "TaxId", "Email", "Phone", "Website", "Address") VALUES
    ('rls-a', 'A', '', '', '', '', '', ''), ('rls-b', 'B', '', '', '', '', '', '');
INSERT INTO "Quotes" ("Id", "OwnerId", "ClientId", "Title", "Status", "ValidUntil", "Discount", "Terms", "CreatedAt") VALUES
    ('00000000-0000-0000-0000-000000000003', 'rls-a', '00000000-0000-0000-0000-000000000001', 'Visível', 'Enviado', CURRENT_DATE, 0, '', now()),
    ('00000000-0000-0000-0000-000000000004', 'rls-a', '00000000-0000-0000-0000-000000000001', 'Rascunho', 'Rascunho', CURRENT_DATE, 0, '', now());
INSERT INTO "Projects" ("Id", "OwnerId", "ClientId", "Title", "Stage", "DueDate", "Value", "ClientPending", "CreatedAt") VALUES
    ('00000000-0000-0000-0000-000000000005', 'rls-a', '00000000-0000-0000-0000-000000000001', 'A', 'Briefing', CURRENT_DATE, 0, '', now()),
    ('00000000-0000-0000-0000-000000000006', 'rls-b', '00000000-0000-0000-0000-000000000002', 'B', 'Briefing', CURRENT_DATE, 0, '', now());
INSERT INTO "ProjectTasks" ("Id", "ProjectId", "Title", "DueDate", "Done") VALUES
    ('00000000-0000-0000-0000-000000000007', '00000000-0000-0000-0000-000000000005', 'Interna', CURRENT_DATE, false);
INSERT INTO "ProjectUpdates" ("Id", "ProjectId", "Text", "Internal", "CreatedAt") VALUES
    ('00000000-0000-0000-0000-000000000008', '00000000-0000-0000-0000-000000000005', 'Compartilhada', false, now()),
    ('00000000-0000-0000-0000-000000000009', '00000000-0000-0000-0000-000000000005', 'Interna', true, now());

SET LOCAL ROLE talume_app;
DO $$
BEGIN
    IF (SELECT count(*) FROM "Projects") <> 0 THEN RAISE EXCEPTION 'Missing identity must deny access'; END IF;

    PERFORM set_config('talume.role', 'Freelancer', true), set_config('talume.user_id', 'rls-a', true);
    IF (SELECT count(*) FROM "Projects") <> 1 OR (SELECT count(*) FROM "Quotes") <> 2 THEN RAISE EXCEPTION 'Developer tenant isolation failed'; END IF;
    UPDATE "Projects" SET "Title" = 'Forbidden' WHERE "OwnerId" = 'rls-b';
    IF FOUND THEN RAISE EXCEPTION 'Cross-tenant update allowed'; END IF;
    BEGIN
        UPDATE "Projects" SET "ClientId" = '00000000-0000-0000-0000-000000000002';
        RAISE EXCEPTION 'Cross-tenant client assignment allowed';
    EXCEPTION WHEN insufficient_privilege THEN
        NULL;
    END;

    PERFORM set_config('talume.role', 'Client', true), set_config('talume.user_id', 'rls-c', true);
    IF (SELECT count(*) FROM "Projects") <> 1 OR (SELECT count(*) FROM "Quotes") <> 1
        OR (SELECT count(*) FROM "ProjectTasks") <> 0 OR (SELECT count(*) FROM "ProjectUpdates") <> 1
        OR (SELECT count(*) FROM "BusinessProfiles") <> 1 THEN
        RAISE EXCEPTION 'Client scope or internal-data protection failed';
    END IF;
    DELETE FROM "Projects";
    IF FOUND THEN RAISE EXCEPTION 'Client delete allowed'; END IF;

    PERFORM set_config('talume.role', 'Freelancer', true), set_config('talume.user_id', 'rls-b', true);
    IF NOT EXISTS (SELECT 1 FROM "Projects" WHERE "OwnerId" = 'rls-b') OR EXISTS (SELECT 1 FROM "Projects" WHERE "OwnerId" = 'rls-a') THEN
        RAISE EXCEPTION 'Changing identity did not change scope';
    END IF;
END $$;
ROLLBACK;
