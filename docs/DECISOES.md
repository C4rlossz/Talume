# Decisões da primeira versão

- Projeto: Talume, nome definido por Carlos. Disponibilidade de marca/domínio não foi verificada.
- Objetivo: aplicação educacional utilizável por freelancers, com acompanhamento somente de leitura para clientes.
- Linguagem principal: C# em .NET 10. Banco principal PostgreSQL. Docker para execução no Windows.
- Interface: Razor Pages e JavaScript, sem empacotador Node. CSS próprio mantém uma identidade consistente.
- O projeto é um monólito modular inicial: um processo web com módulos de autenticação e negócios. Facilita estudar o fluxo completo antes de dividir em serviços.
- Banco local criado com EnsureCreated, explicitamente ativado no Compose. O SQL de referência é gerado com Npgsql. Evoluções sobre dados reais devem introduzir migrations versionadas.
- SQLite existe somente para testes locais sem Docker; não é a configuração de produção.
- PDFs usam a impressão do navegador de uma página Razor autorizada. Não há um gerador de PDF no backend.
- Entregas são links HTTPS; armazenamento privado de arquivos está fora desta primeira versão.
- Aprovação é registrada pelo freelancer após combinar com o cliente; não equivale a assinatura eletrônica.
- E-mails de cadastro/recuperação funcionam por SMTP. No Docker são capturados pelo Mailpit.
- WhatsApp: mensagem manual preparada. Automação oficial é uma evolução futura.
- Clientes não editam etapas, preços ou dados de pagamento. As permissões são impostas na API.
- Um cliente pode ser convidado para mais de um freelancer usando o mesmo e-mail de cliente; cada convite precisa de confirmação.
- Na versão inicial, uma conta de freelancer não assume simultaneamente o papel de cliente. Usar e-mails distintos para esses papéis.
- Repositório privado definido em 16/09/2026: `C4rlossz/Talume`. Publicação no Railway ainda pendente.

## Identidade comercial e atualização 002

`BusinessProfiles` tem chave `OwnerId` vinculada à conta Identity. GET/PUT exigem a política Freelancer; o servidor obtém a chave da sessão e ignora IDs de proprietário enviados no corpo. Documentos consultam a empresa do dono do orçamento após verificar o acesso ao orçamento. Razor codifica os dados; a prévia também escapa texto. Não preenchemos identificadores fiscais ou contatos fictícios para Origins.

`SchemaUpdates.ApplyLocalAsync` acrescenta a tabela via `CREATE TABLE IF NOT EXISTS` quando a inicialização do banco está habilitada. É uma atualização aditiva específica, não um sistema geral de migrações. O SQL equivalente está em `upgrade-002-business-profile.sql`; produção aplica o script previamente, com backup.

O perfil é lido no momento de abrir a proposta. Uma versão futura poderá guardar o retrato dos dados comerciais por emissão; PDFs baixados já são cópias estáticas. Condições de pagamento continuam livres no texto e não são interpretadas para criar parcelas.

## Atualização 003: apresentação, convites e logos

O produto exibe Desenvolvedor; o identificador Identity `Freelancer` é preservado para não invalidar papéis ou contas existentes. O login de demonstração também é preservado.

`BusinessLogos` é vinculada à conta e `QuoteLogos` ao orçamento. Os endpoints da marca usam a sessão autenticada, sem aceitar OwnerId do cliente. A proposta só retorna a imagem após validar o acesso ao orçamento. Os dados são PNGs limitados e verificados no servidor, em texto base64 no PostgreSQL. Para essa escala inicial, o armazenamento junto do banco simplifica backup e evita diretórios voláteis; a duplicação por orçamento deverá ser revista se o volume de propostas aumentar.

O navegador normaliza PNGs com canvas preservando alfa. Novas propostas copiam a logo; uma string vazia representa a escolha explícita de não usar logo. Null em clientes antigos da API usa a logo atual da empresa. Propostas existentes sem `QuoteLogos` usam a logo da empresa como compatibilidade.

O convite SMTP é multipart/alternative com texto e HTML. O HTML usa tabelas e estilos inline; duas imagens PNG originais são incorporadas por Content-ID. Nomes e URLs são codificados para HTML. A API de Resend não faz parte desta atualização.

O Compose distribuído agora usa localhost:8081, preservando a correção da porta feita durante o teste do usuário; a aplicação dentro do contêiner continua em 8080. O script SQL 003 é aditivo e executado na inicialização local após 002. Produção deve aplicar os scripts previamente.

## Atualização 004: Resend e piloto

O transporte agora seleciona Resend HTTPS em produção e SMTP no desenvolvimento. O cliente HTTP tem timeout e não registra conteúdo de mensagens ou credenciais. Falhas invalidam o desafio/convite e retornam 503. Testes com HTTP simulado bloqueiam o build em caso de regressão. O cadastro de desenvolvedores em produção usa uma lista de e-mails autorizados, vazia por padrão; a regra de convite do cliente permanece. Detalhes em [RESEND-RAILWAY.md](RESEND-RAILWAY.md).
