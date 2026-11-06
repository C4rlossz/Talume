# Operação segura do Talume

## Segredos e banco

- Configure `Resend__ApiKey` e `ConnectionStrings__Default` exclusivamente nas variáveis do ambiente. Sele a chave do Resend no painel do Railway para impedir a leitura depois de configurada. O navegador consome a API do Talume, que consulta o PostgreSQL e envia e-mails no servidor. Não há chave pública de banco para o navegador.
- Mantenha o PostgreSQL na rede privada do Railway, sem TCP proxy público. Use TLS na conexão e valide o certificado se a configuração do PostgreSQL disponibilizar uma CA compatível. Mantenha o volume de chaves de Data Protection restrito ao serviço Talume e inclua-o na estratégia de recuperação.
- O Railway documenta criptografia em repouso de volumes, backups e variáveis. O Talume usa ASP.NET Core Identity para o hash das senhas, Data Protection para cookies e para proteger o verificador dos novos códigos de e-mail. Criptografia por campo para dados comerciais já cadastrados requer migração própria e política de rotação de chaves.
- Se um segredo aparecer em uma varredura, revogue e substitua a credencial antes de tratar o histórico. Coordene eventual reescrita do Git com os colaboradores e trate cópias, forks e caches separadamente. Nunca cole o valor em uma issue ou log.

## RLS no PostgreSQL

Faça o ensaio em banco restaurado ou descartável antes da produção. O esquema existente é compatível com o script `upgrade-004-tenant-rls.sql`. O isolamento cobre projetos, orçamentos, serviços, perfis, logos e seus registros dependentes. Contas, clientes, convites e desafios continuam com as verificações de autenticação e vínculo feitas pelo servidor, inclusive durante o cadastro por convite.

1. Valide um backup restaurável e aplique o script como proprietário do esquema, com uma função de aplicação separada. Crie o login sem privilégios administrativos com `CREATE ROLE talume_app LOGIN NOINHERIT;` e configure a senha fora do histórico SQL com `\password talume_app` no `psql`.
2. Defina `Database__TenantPolicies=true` e `Database__Initialize=false` no serviço. Troque a conexão da aplicação para `talume_app` usando as variáveis protegidas. Preserve a conexão administrativa para migrações. Mantenha o reset padrão do pool Npgsql e a multiplexação desligada.
3. Confira com duas contas independentes que listas, documentos, portal, convites, recuperação, exclusões e leitura de tarefas internas obedecem ao vínculo. Em SQL, valide com o papel `talume_app`, pois o dono e os superusuários podem ignorar RLS.
4. Se a ativação falhar, restaure a configuração de conexão anterior e confira os fluxos. Mantenha o banco e os volumes existentes.

## HTTPS, limites e proteção de borda

- O Railway encerra TLS na borda e redireciona tráfego HTTP. A aplicação envia HSTS em produção e marca os cookies como seguros. Só configure `Security__TrustedProxies__0` e valores seguintes quando os IPs forem conhecidos e controlados. Não aceite cabeçalhos de proxy arbitrários.
- Autenticação tem limite por origem, limite de concorrência e bloqueio temporário por conta. O restante da API tem limite por conta e as rotas exigem métodos explícitos, autorização e proteção antifalsificação nas alterações. O [WAF do Railway](https://docs.railway.com/networking/waf) oferece um desafio de navegador durante ataques. Após ativar uma regra de borda, valide login, convites e recuperação.
- O único upload no aplicativo é PNG de logo. A aplicação valida o conteúdo e limita o tamanho. Entregas continuam por link HTTPS. Não exponha diretórios de dados pelo servidor estático.
- O pipeline verifica dependências NuGet e segredos do Git. Em caso de alerta, trate a atualização ou a revogação antes de publicar a versão.
