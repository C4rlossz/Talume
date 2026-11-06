-- Run as the schema owner. Provision the separate talume_app login before running this file.
BEGIN;

DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_roles WHERE rolname = 'talume_app' AND rolcanlogin
        AND NOT (rolsuper OR rolbypassrls OR rolcreaterole OR rolcreatedb OR rolreplication OR rolinherit)) THEN
        RAISE EXCEPTION 'Provision a restricted talume_app login before applying this upgrade';
    END IF;
    IF EXISTS (SELECT 1 FROM pg_auth_members WHERE member = 'talume_app'::regrole)
        OR EXISTS (SELECT 1 FROM pg_class WHERE relnamespace = 'public'::regnamespace AND relowner = 'talume_app'::regrole)
        OR EXISTS (SELECT 1 FROM pg_namespace WHERE nspname = 'public' AND nspowner = 'talume_app'::regrole)
        OR EXISTS (SELECT 1 FROM pg_database WHERE datname = current_database() AND datdba = 'talume_app'::regrole)
        OR has_schema_privilege('talume_app', 'public', 'CREATE') THEN
        RAISE EXCEPTION 'talume_app must not own objects, create schema objects or belong to another role';
    END IF;
END $$;

GRANT USAGE ON SCHEMA public TO talume_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON TABLE
    public."AspNetRoles", public."AspNetRoleClaims", public."AspNetUsers",
    public."AspNetUserClaims", public."AspNetUserLogins", public."AspNetUserRoles", public."AspNetUserTokens",
    public."Clients", public."Invitations", public."EmailChallenges", public."CatalogServices",
    public."BusinessProfiles", public."BusinessLogos", public."Quotes", public."QuoteLine",
    public."QuoteLogos", public."Projects", public."ProjectTasks", public."ProjectUpdates",
    public."Deliveries", public."Installments" TO talume_app;
GRANT USAGE ON SEQUENCE public."AspNetRoleClaims_Id_seq", public."AspNetUserClaims_Id_seq" TO talume_app;

DO $$
DECLARE
    item text;
    actor text := 'current_setting(''talume.user_id'', true)';
    developer text := 'current_setting(''talume.role'', true) = ''Freelancer''';
    customer text := 'current_setting(''talume.role'', true) = ''Client''';
    owns text;
    linked text;
BEGIN
    FOREACH item IN ARRAY ARRAY['CatalogServices', 'BusinessProfiles', 'BusinessLogos', 'Quotes', 'Projects']
    LOOP
        owns := format('%s AND "OwnerId" = %s', developer, actor);
        linked := owns;
        IF item IN ('Quotes', 'Projects') THEN
            linked := linked || format(' AND EXISTS (SELECT 1 FROM public."Clients" c WHERE c."Id" = "ClientId" AND c."OwnerId" = %s)', actor);
        END IF;
        IF item = 'Projects' THEN
            linked := linked || ' AND ("QuoteId" IS NULL OR EXISTS (SELECT 1 FROM public."Quotes" q WHERE q."Id" = "Projects"."QuoteId" AND q."ClientId" = "Projects"."ClientId"))';
        END IF;
        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', item);
        EXECUTE format('DROP POLICY IF EXISTS talume_owner ON public.%I', item);
        EXECUTE format('CREATE POLICY talume_owner ON public.%I TO talume_app USING (%s) WITH CHECK (%s)', item, owns, linked);
    END LOOP;

    DROP POLICY IF EXISTS talume_customer ON public."Quotes";
    EXECUTE format('CREATE POLICY talume_customer ON public."Quotes" FOR SELECT TO talume_app USING (%s AND "Status" <> ''Rascunho'' AND EXISTS (SELECT 1 FROM public."Clients" c WHERE c."Id" = "ClientId" AND c."UserId" = %s))', customer, actor);
    DROP POLICY IF EXISTS talume_customer ON public."Projects";
    EXECUTE format('CREATE POLICY talume_customer ON public."Projects" FOR SELECT TO talume_app USING (%s AND EXISTS (SELECT 1 FROM public."Clients" c WHERE c."Id" = "ClientId" AND c."UserId" = %s))', customer, actor);

    FOREACH item IN ARRAY ARRAY['BusinessProfiles', 'BusinessLogos']
    LOOP
        EXECUTE format('DROP POLICY IF EXISTS talume_customer ON public.%I', item);
        EXECUTE format('CREATE POLICY talume_customer ON public.%I FOR SELECT TO talume_app USING (%s AND EXISTS (SELECT 1 FROM public."Quotes" q WHERE q."OwnerId" = %I."OwnerId"))', item, customer, item);
    END LOOP;

    FOREACH item IN ARRAY ARRAY['ProjectTasks', 'ProjectUpdates', 'Deliveries', 'Installments', 'QuoteLine', 'QuoteLogos']
    LOOP
        linked := CASE WHEN item IN ('QuoteLine', 'QuoteLogos')
            THEN 'EXISTS (SELECT 1 FROM public."Quotes" q WHERE q."Id" = "QuoteId")'
            ELSE 'EXISTS (SELECT 1 FROM public."Projects" p WHERE p."Id" = "ProjectId")' END;
        EXECUTE format('ALTER TABLE public.%I ENABLE ROW LEVEL SECURITY', item);
        EXECUTE format('DROP POLICY IF EXISTS talume_owner ON public.%I', item);
        EXECUTE format('CREATE POLICY talume_owner ON public.%I TO talume_app USING (%s AND %s)', item, developer, linked);
        IF item <> 'ProjectTasks' THEN
            IF item = 'ProjectUpdates' THEN linked := linked || ' AND NOT "Internal"'; END IF;
            EXECUTE format('DROP POLICY IF EXISTS talume_customer ON public.%I', item);
            EXECUTE format('CREATE POLICY talume_customer ON public.%I FOR SELECT TO talume_app USING (%s AND %s)', item, customer, linked);
        END IF;
    END LOOP;
END $$;

COMMIT;
