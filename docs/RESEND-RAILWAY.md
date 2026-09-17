# Resend e piloto no Railway

Atualização de 16/09/2026, desenvolvida por Carlos com apoio de ChatGPT/Codex.

## Situação da publicação

- [GitHub privado](https://github.com/C4rlossz/Talume): integração enviada à branch `main`.
- [Railway](https://railway.com/project/0c4fb946-f4ed-4ef5-9028-77d9de9351df): PostgreSQL online, serviço Talume e volumes criados.
- Aplicação **ainda offline**, sem primeiro deployment: o Railway relatou que o repositório não foi encontrado ou não está acessível. A causa exata na instalação GitHub precisa ser conferida pelo proprietário.
- Domínio reservado: `talume-production.up.railway.app`. A reserva não comprova publicação nem funcionamento.
- Chave Resend, remetente autorizado e lista de e-mails de desenvolvedores ainda precisam ser preenchidos.
- O workflow [Validate Talume](https://github.com/C4rlossz/Talume/actions) compila e verifica o transporte, a suíte HTTP/SMTP e a imagem Docker; inclui também execução descartável com PostgreSQL e teste de persistência das chaves. Não testa entrega real do Resend.

Para liberar o deploy, abra GitHub → Settings → Applications → Installed GitHub Apps → Railway → Configure e conceda acesso especificamente a `Talume`. Se o Railway não estiver instalado, conecte sua conta GitHub pelo painel Railway e instale-o para esse repositório. No serviço Talume, confira a origem `C4rlossz/Talume`, branch `main`, e inicie o primeiro deploy. Não é necessário tornar o repositório público.

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

- Origem: repositório privado `C4rlossz/Talume`, branch `main`, Dockerfile.
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
