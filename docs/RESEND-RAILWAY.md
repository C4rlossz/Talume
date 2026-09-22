# Resend e piloto no Railway

Atualização de 16/09/2026, desenvolvida por Carlos com apoio de ChatGPT/Codex.

## Situação da publicação — 22/09/2026

- Código versionado em [C4rlossz/Talume](https://github.com/C4rlossz/Talume), branch `main`.
- Aplicação e PostgreSQL publicados no Railway. Domínio próprio: `https://talume.app.br`.
- DNS delegado à Cloudflare. A validação do domínio no Railway inclui CNAME e TXT `_railway-verify`.
- Integração Resend implementada para confirmação de clientes e desenvolvedores, recuperação de senha e convites.
- A autenticação de `talume.app.br` no Resend ainda depende da publicação e verificação dos registros abaixo. Não confundir site online com envio de e-mail liberado.
- O workflow [Validate Talume](https://github.com/C4rlossz/Talume/actions) compila e verifica o transporte, a suíte HTTP/SMTP e a imagem Docker. Não comprova entrega real de e-mails.

### Finalizar o domínio de e-mail

Importe [talume-resend-dns.txt](talume-resend-dns.txt) em Cloudflare → DNS → Records → Import.
Desmarque a opção de aplicar proxy aos registros importados, se aparecer.
O arquivo acrescenta DKIM (TXT), SPF (TXT), MX de envio e CNAME de autenticação; mantenha os registros existentes do site e a política DMARC.
O SPF `v=spf1 -all` na raiz não substitui o SPF do subdomínio `send`; não adicione um segundo SPF na raiz.
Não há criação de caixa de entrada: o MX em `send` é usado pelo provedor para o envio.

Depois da importação, execute a verificação em Resend → Domains → talume.app.br.
Somente após o status **Verified**, configure no serviço Talume:

- `Mail__From=Talume <nao-responda@talume.app.br>`
- `Mail__Provider=Resend`
- `App__BaseUrl=https://talume.app.br`

A chave de envio existente permanece apenas no Railway. Nunca a coloque no arquivo DNS.
Reaplique as variáveis com deploy e valide entrega real para endereços autorizados antes de considerar o fluxo concluído.

### Autoria e LinkedIn

A janela “Sobre o Talume” descreve as tecnologias efetivamente usadas.
Defina `App__AuthorLinkedIn` no Railway com a URL HTTPS completa do perfil de Carlos Eduardo, no formato `https://www.linkedin.com/in/...`.
Somente URLs de perfil em `linkedin.com` ou `www.linkedin.com` são aceitas.
Sem um perfil válido, o nome continua visível como texto, sem link vazio ou endereço inventado.

As configurações Dockerfile, porta, healthcheck e volume foram aplicadas diretamente ao serviço. O Railway informou que `railway.toml` é legado; este piloto não depende de editar esse arquivo para configurar o serviço.

## E-mails

`MailSender` envia confirmação, recuperação de senha e convites pela API HTTPS do Resend em produção. O ambiente Development continua usando SMTP/Mailpit. Convites preservam texto, HTML e as duas imagens CID.

### Código com a identidade do Talume — 17/09/2026

Clientes e desenvolvedores recebem o código em um e-mail com a mesma logo, ilustração e paleta do convite. A recuperação de senha usa o mesmo modelo com assunto e instruções próprios. O código permanece em texto selecionável, com alternativa de texto simples, validade de dez minutos e uso único. O HTML não confirma a conta sozinho: o usuário informa o código na tela do Talume e o servidor valida o desafio.

A [prévia de confirmação](../preview/Talume-codigo-previa.html) contém o nome Carlos e o código fictício `123456`; não é um código válido. Para regenerar a partir do template real:

```sh
dotnet run --project tests/Talume.MailTests -c Release -- --export-preview preview/Talume-codigo-previa.html
```

O CI confere que a prévia corresponde ao template C#, que Resend recebe HTML/texto/imagens e que os fluxos de cliente, desenvolvedor e recuperação geram o e-mail com código pelo SMTP local. Não há envio a pessoas reais nos testes.

Ter Resend integrado ao código não configura a conta do provedor. É necessário criar uma chave de envio e verificar um domínio próprio no Resend para enviar aos clientes. O remetente de teste `onboarding@resend.dev` só serve para testes destinados ao e-mail da própria conta Resend. Configure `Resend__ApiKey` e `Mail__From` diretamente nas variáveis do serviço Railway; os valores são ocultos para o conector, portanto a existência dos nomes não comprova que estejam preenchidos ou válidos.

Configure no serviço **Talume**, em **Variables** do Railway:

| Variável | Valor |
|---|---|
| `Mail__Provider` | `Resend` |
| `Resend__ApiKey` | Chave de envio criada na sua conta Resend; cadastrar apenas no Railway |
| `Mail__From` | `Talume <contato@seu-dominio-verificado>` (substituir pelo remetente real) |
| `App__BaseUrl` | URL HTTPS pública do Talume |
| `Registration__AllowPublicFreelancers` | `false` |
| `Registration__AllowedEmails` | Seu e-mail de cadastro; separar outros autorizados por `;` |

Use um domínio verificado no Resend para enviar a clientes. O domínio de teste `resend.dev` tem restrições de destinatário. Nunca copie a chave para o código, README, chat ou arquivos versionados.

Sem chave/remetente, o site pode abrir, mas os envios retornam falha controlada. Não existe confirmação automática de contas nem envio por SMTP como alternativa silenciosa. A lista de desenvolvedores começa vazia em produção; clientes continuam entrando por convite. Isso permite publicar o piloto enquanto o proprietário termina de configurar o e-mail.

Rejeições da API, indisponibilidade da rede e timeout de 20 segundos são tratados sem expor chaves ou conteúdo de mensagens. Códigos/convites de envios malsucedidos são invalidados, permitindo nova tentativa. O transporte não repete automaticamente uma requisição, evitando envios duplicados em respostas incertas.

## Infraestrutura

- Origem: repositório `C4rlossz/Talume`, branch `main`, Dockerfile.
- Ambiente: `Production`, `Demo__Seed=false`, `Database__Provider=Postgres`.
- Conexão Npgsql: `Host=${{Postgres.PGHOST}};Port=${{Postgres.PGPORT}};Database=${{Postgres.PGDATABASE}};Username=${{Postgres.PGUSER}};Password=${{Postgres.PGPASSWORD}};SSL Mode=Require`.
- PostgreSQL acessível somente pela rede privada, com volume persistente. O template disponível no Railway usa PostgreSQL 18; o Compose local permanece na versão 17.
- Domínio público encaminhado à porta 8080; `AllowedHosts` deve incluir o domínio e `healthcheck.railway.app` para a verificação do Railway.
- Volume da aplicação em `/app/keys`, `DataProtection__Path=/app/keys`. O entrypoint ajusta a permissão do volume e executa o processo .NET com o usuário sem privilégios `app`.
- Primeira inicialização **somente em banco vazio**: `Database__Initialize=true`. Após o primeiro início bem-sucedido, alterar para `false`. Isso não substitui migrations versionadas; não usar para atualizar bancos com dados reais.
- Sem mudança de plano: a conta conectada informou limite de zero backups de volume. Backup externo e teste de restauração continuam pendentes antes do uso com dados reais.

## Verificação

O Dockerfile executa `dotnet run --project tests/Talume.MailTests -c Release` antes de publicar. Os testes usam HTTP simulado: verificam payload, imagens, erros HTTP 401/403/429/500, rede, timeout, configuração ausente e restrição de cadastros. Não usam chave real nem enviam e-mails. Um teste com falha impede o build.

Para repetir localmente a partir da raiz:

```sh
dotnet run --project tests/Talume.MailTests -c Release
dotnet build src/Talume.Web
python tests/smoke.py --local dotnet
```

Depois de configurar o Resend, validar manualmente com endereços controlados: cadastro autorizado → código → confirmação → login; convite de cliente → confirmação → portal; recuperação de senha. Conferir a entrega na caixa e no painel Resend. O build não comprova a entrega real de mensagens.

## Documentação dos provedores

- [API de envio Resend](https://resend.com/docs/api-reference/emails/send-email)
- [Imagens incorporadas no Resend](https://resend.com/docs/dashboard/emails/embed-inline-images)
- [ASP.NET Core no Railway](https://docs.railway.com/guides/aspnet-core)

Migrations completas, restauração de backup, simplificação da senha no cadastro e correções de pagamentos com histórico permanecem como próximas etapas.
