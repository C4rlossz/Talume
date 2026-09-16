# Talume

Organização de serviços para desenvolvedores, com um portal de acompanhamento para clientes.

Projeto pessoal de **Carlos (@C4rlossz)**, em desenvolvimento para organizar seus serviços de criação de sites e oferecer aos clientes um portal de acompanhamento. Também faz parte de sua formação prática e de seu portfólio em C# e .NET.

**Data deste registro: 16 de setembro de 2026.** Registro da versão inicial no repositório privado [C4rlossz/Talume](https://github.com/C4rlossz/Talume). Esta data não representa o início do projeto; a publicação da aplicação no Railway ainda está pendente.

O Talume é desenvolvido por Carlos **com apoio de inteligência artificial, incluindo ChatGPT/Codex (OpenAI)**, na elaboração e revisão de código, documentação, explicações e planejamento de melhorias. Carlos conduz as decisões sobre o produto e sua evolução. O código precisa ser compreendido, revisado e testado antes do uso com clientes reais.

Dados da demonstração são fictícios. Não há métricas de uso ou clientes reais implícitos neste repositório.

### Situação em 16/09/2026

- Base em ASP.NET Core / .NET 10, PostgreSQL e Docker, com áreas de desenvolvedor e cliente.
- Cadastro, confirmação de e-mail, recuperação de senha, clientes, serviços, propostas, projetos, tarefas e registro manual de pagamentos implementados.
- Envio por API HTTPS do Resend em produção e SMTP/Mailpit no ambiente local. Ativação real depende da chave e do remetente autorizado.
- Cadastro de desenvolvedores restrito por lista autorizada em produção; clientes continuam por convite. Próximas melhorias: simplificação do cadastro, migrations versionadas, backup com teste de restauração e correção de pagamentos com histórico.
- Publicação no Railway e validação do piloto ainda pendentes.

### Descrição curta para o GitHub

Talume — gestão de projetos, clientes, propostas e pagamentos para desenvolvedores. Projeto de Carlos em C#/.NET, desenvolvido com apoio de IA (ChatGPT/Codex). Registro: 16/09/2026.

### Cuidados com o repositório

Mantenha o repositório privado nesta etapa. Configure senhas e chaves reais somente no ambiente local ou nas variáveis da hospedagem. O arquivo `.env.example` contém apenas um exemplo de desenvolvimento. Não envie arquivos `.env`, bancos locais, backups ou chaves de sessão. As credenciais de demonstração documentadas abaixo são exclusivas do ambiente local.

## Rodar no Windows com Docker Desktop

Você só precisa do Docker Desktop aberto, usando contêineres Linux. Não precisa instalar .NET ou PostgreSQL separadamente neste caminho.

1. Extraia o ZIP, por exemplo em `C:\Projetos\Talume`.
2. Abra o PowerShell nessa pasta (a que contém `compose.yaml`).
3. Execute:

```powershell
docker compose up --build -d
```

O primeiro início baixa as imagens e compila o projeto; aguarde alguns minutos. Depois abra:

- Aplicação: http://localhost:8081
- Caixa de e-mails de teste (Mailpit): http://localhost:8025

Também é possível executar `iniciar-windows.cmd` dentro da pasta extraída.

| Perfil de demonstração | E-mail | Senha |
|---|---|---|
| Desenvolvedor | freelancer@talume.local | TalumeDemo2026! |
| Cliente | cliente@talume.local | TalumeDemo2026! |

O cliente de demonstração vê apenas o projeto do Estúdio Aurora. Experimente os dois perfis em janelas separadas ou saia antes de trocar de conta.

### Comandos úteis

```powershell
# Ver estado e logs
docker compose ps
docker compose logs --tail=100 app

# Parar sem apagar os dados
docker compose down

# Iniciar novamente após alterar o código
docker compose up --build -d
```

O volume `pg_data` preserva o banco ao parar os contêineres. O volume `app_keys` preserva as chaves de proteção das sessões. Não use `down -v` se quiser manter seus dados: essa opção remove os volumes.

As portas são vinculadas a 127.0.0.1 para uso local. PostgreSQL não é exposto diretamente ao computador. Se a porta 8081 estiver ocupada, altere a porta externa em `compose.yaml` e também `App__BaseUrl`. Se 8025 estiver ocupada, altere a porta externa do Mailpit.

## Temas claro e escuro

Use o botão de tema no topo da tela para alternar entre o visual original e o tema escuro em preto e branco, com a logo verde. A preferência fica salva neste navegador, vale também para login e portal do cliente e permanece ao trocar de página. PDFs e impressão continuam com fundo claro.

## Sua empresa e as propostas

Abra **Minha empresa → Editar dados**. O perfil de demonstração usa **Origins**, com Carlos como responsável. Outras contas começam com o nome do desenvolvedor e podem cadastrar sua própria marca. Nome é obrigatório; responsável, CPF/CNPJ, e-mail comercial, telefone, site e endereço são opcionais. Campos vazios não aparecem no documento. O CPF/CNPJ é um campo de identificação informado pelo usuário, sem verificação cadastral automática.

O documento apresenta prestador, cliente, referência, emissão, validade, itens, subtotal, desconto, investimento total e condições. O Talume aparece discretamente no rodapé como plataforma. Os dados do prestador são consultados pela conta que criou a proposta, inclusive quando um cliente abre o documento.

As condições continuam editáveis por orçamento; o padrão permanece **50% na aprovação e 50% na entrega**, com duas rodadas de revisão. Esses percentuais são texto comercial: não criam parcelas automaticamente. A aprovação ainda é registrada pelo desenvolvedor após combinar com o cliente; o documento não contém assinatura eletrônica ou aceite automático.

Em **Orçamentos → Ver / PDF → Imprimir / Salvar PDF**, selecione A4, “Salvar como PDF” e desative os cabeçalhos e rodapés do navegador para remover URL e data automáticas. O documento usa fundo claro para impressão. Confira a prévia de impressão antes de enviar, especialmente quando houver muitos itens.

Alterar **Minha empresa** atualiza o cabeçalho ao reabrir propostas anteriores. PDFs já baixados não mudam. Congelar a identidade do prestador por versão emitida é uma evolução futura.

### Logo transparente no orçamento

Em **Minha empresa → Adicionar logo**, escolha um PNG transparente e salve. Também há um campo de imagem em **Novo orçamento**: a logo da empresa aparece inicialmente, e você pode trocar ou remover apenas para aquela proposta. A imagem aparece ao lado do nome da empresa no documento e no PDF.

O arquivo de entrada pode ter até 2 MB; o navegador preserva a transparência e ajusta a imagem para no máximo 1200 pixels por lado. A versão enviada deve ter até 512 KB. O servidor valida assinatura, estrutura, CRC e raster PNG, com limite de 2000 × 2000 pixels. Imagens ficam no PostgreSQL e entram no backup do banco.

Novos orçamentos guardam uma cópia da logo escolhida; mudar a logo da empresa não altera essas cópias. Orçamentos criados antes desta atualização, sem cópia de logo, usam a logo atual da empresa. Os dados escritos do prestador continuam sendo lidos do cadastro atual.

### Convite com a identidade do Talume

Em **Clientes → Convidar**, o e-mail agora tem saudação com o nome do cliente, logo do Talume, ilustração de etapas, botão “Acessar meus projetos”, link alternativo e validade de sete dias. A ilustração e o símbolo são PNGs incorporados ao e-mail via CID, com uma alternativa em texto simples. Não dependem de URLs públicas de imagens. Alguns clientes de e-mail podem bloquear a exibição de imagens; o botão, o link e o texto continuam disponíveis.

Para conferir, cadastre um cliente de teste ainda sem acesso e abra o novo convite no Mailpit em `http://localhost:8025`. E-mails recebidos antes da atualização mantêm o visual anterior. O envio real usa Resend/API após configurar a chave e um remetente autorizado; veja [configuração](docs/RESEND-RAILWAY.md).

A demonstração visual do convite está em `preview/Talume-convite-previa.html`. O botão dessa prévia é ilustrativo; os e-mails enviados pelo aplicativo têm o link real do convite.

O nome do perfil agora é **Desenvolvedor** nas telas. O identificador interno `Freelancer` e o e-mail de demonstração `freelancer@talume.local` foram mantidos por compatibilidade com as contas e permissões existentes.

### Atualizar uma instalação local existente

Faça backup dos seus dados. Substitua os arquivos do projeto na **mesma pasta** e execute `docker compose up --build -d`. Preserve os volumes e o nome do projeto Compose. As atualizações acrescentam `BusinessProfiles`, `BusinessLogos` e `QuoteLogos`, sem apagar clientes, propostas ou projetos. O perfil Origins é criado apenas para a conta de demonstração, se ela ainda não tiver perfil; personalizações existentes não são sobrescritas.

Em produção, com `Database__Initialize=false`, aplique, na ordem, `docs/upgrade-002-business-profile.sql` e `docs/upgrade-003-logos.sql` antes de iniciar esta versão. Para um banco novo e vazio, o `docs/schema-postgres.sql` já inclui a tabela. Não reaplique o esquema completo sobre um banco existente.

## Primeiros passos no produto

1. Entre como desenvolvedor, personalize **Minha empresa** e abra **Clientes**.
2. Cadastre nome, e-mail e, se quiser usar o botão de WhatsApp, telefone com DDI e DDD (Brasil: 55).
3. Cadastre ou edite um serviço com preço base e prazo estimado.
4. Crie um orçamento com um ou mais itens; confira quantidade, preço, desconto e condições.
5. Use **Compartilhar** para exibi-lo no portal do cliente. Isso não envia o orçamento por e-mail automaticamente.
6. Use **Ver / PDF** e a impressão do navegador para salvar um PDF real. A renderização final do PDF é feita pelo navegador.
7. Após combinar a aprovação com o cliente, use **Aprovar** para gerar um projeto.
8. Adicione tarefas, atualizações, links de entrega e parcelas.
9. Em **Clientes**, clique em **Convidar**. Abra o convite recebido no Mailpit.
10. Cadastre a conta do cliente, leia o código de seis dígitos no Mailpit e confirme a senha de acesso.
11. Entre como cliente: tarefas e notas internas não aparecem; ele não consegue editar o projeto.

O código vale por 10 minutos, é de uso único e aceita até cinco tentativas. Novo envio exige um intervalo mínimo de um minuto. Convites valem por sete dias. Usuários confirmados podem recuperar a senha na tela de acesso.

O desenvolvedor também pode criar sua própria conta pela tela inicial. Essa conta começa vazia e isolada das contas de demonstração.

## E-mails e WhatsApp

**Docker local:** os e-mails chegam somente ao Mailpit. Mesmo ao cadastrar um endereço real, não haverá envio para a caixa externa. Isso permite testar sem contratar um provedor.

**Envio real:** configure `Mail__Provider=Resend`, `Resend__ApiKey` e `Mail__From` com um remetente de domínio verificado no Resend. Veja [Resend e Railway](docs/RESEND-RAILWAY.md). Não coloque credenciais no código nem no Git. Mantenha `App__BaseUrl` igual ao endereço público confiável da aplicação, pois ele aparece nos convites.

**WhatsApp:** o botão prepara uma mensagem e abre o WhatsApp; o desenvolvedor revisa e envia manualmente. Não há disparo automático nem integração com a API oficial nesta versão. Essa automação ficou planejada para uma etapa seguinte.

## Tecnologias e estrutura

| Parte | Implementação |
|---|---|
| Linguagem principal | C# |
| Plataforma | .NET 10 / ASP.NET Core |
| Páginas | Razor Pages, HTML, CSS próprio e JavaScript sem framework |
| Backend | Minimal APIs organizadas por área |
| Contas e senhas | ASP.NET Core Identity, sessão em cookie HttpOnly |
| Banco principal | PostgreSQL 17 |
| Acesso a dados | Entity Framework Core + Npgsql |
| Ambiente local | Docker Compose + Mailpit |
| Documentos | Página Razor autorizada, com impressão / salvar PDF |
| Testes | Python 3, chamadas HTTP reais e fixture SMTP |

O CSS é próprio para manter o visual do Talume consistente e reduzir dependências de interface. SQLite está disponível exclusivamente em Development como alternativa auxiliar de testes; o Compose e a configuração padrão usam PostgreSQL.

```text
src/Talume.Web/
  Program.cs                 Configuração da aplicação, autenticação e pipeline
  Models/Entities.cs         Entidades e regras simples do domínio
  Data/AppDbContext.cs       Relacionamentos, índices e banco
  Data/Seed.cs               Contas e exemplos de demonstração
  Endpoints/AuthEndpoints.cs Cadastro, convite, confirmação e recuperação
  Endpoints/BusinessEndpoints.cs Clientes, serviços, orçamentos e projetos
  Services/Access.cs         Consultas com limites de acesso
  Services/MailSender.cs     Resend HTTPS / SMTP local
  Pages/                    Páginas Razor e documento de orçamento
  wwwroot/app.js             Interações e chamadas ao backend
  wwwroot/app.css            Visual e responsividade
compose.yaml                 Aplicação + PostgreSQL + Mailpit
Dockerfile                   Compilação e execução em contêiner
railway.toml                 Preparação para hospedagem no Railway
docs/schema-postgres.sql     Esquema PostgreSQL gerado pelo EF Core
preview/Talume-previa.html   Prévia independente com dados fictícios
tests/smoke.py               Testes de integração
```

## Prévia independente

Abra `preview/Talume-previa.html` no navegador. Você pode navegar, criar registros de demonstração e trocar entre os dois perfis. Os dados dessa prévia existem apenas na memória: desaparecem ao recarregar. Ela não executa C#, não autentica pessoas e não envia e-mails. A aplicação completa é a que você inicia pelo Docker.

O adaptador de demonstração fica fora de `wwwroot` e não é incluído no contêiner final.

## Regras implementadas

- O cliente recebe acesso pelo convite, confirmado para o e-mail correto.
- Consultas de projeto e orçamento verificam o vínculo no servidor.
- Cada desenvolvedor acessa somente seus clientes, serviços, orçamentos e projetos.
- Tarefas e notas internas nunca são retornadas pela API do cliente.
- Orçamentos em rascunho não aparecem no portal.
- Linhas do orçamento guardam cópias dos preços: editar um serviço não altera negociações anteriores.
- Descontos não podem superar o subtotal; valores monetários usam `decimal`.
- Uma aprovação cria no máximo um projeto por orçamento, inclusive sob concorrência.
- Mudanças de etapa, prazo e pendência entram no histórico compartilhado.
- Parcelas não podem ultrapassar o valor do projeto. Registrar pagamento duas vezes não duplica recebimento.
- Entregar um projeto não marca parcelas como pagas automaticamente.
- Documentos exigem autenticação e acesso ao orçamento correspondente.
- Todas as alterações via API, inclusive login, exigem token antifalsificação de requisições (CSRF).
- Login tem limite de tentativas; convites e códigos expiram.

## Validação

A aplicação foi compilada em .NET 10, sem erros ou avisos no build final. Foram executados testes HTTP contra a aplicação real, com SQLite auxiliar e servidor SMTP de teste, cobrindo autenticação, isolamento de dados, códigos, recuperação de senha, cálculos, conversão, tarefas, parcelas, cadastro de empresa e página de documento. O cadastro e as logos foram testados quanto ao isolamento entre contas, rejeição de PNGs inválidos, preservação da logo por orçamento e codificação de texto no HTML. O convite foi verificado no SMTP de teste com partes HTML/texto e duas imagens CID. A publicação Release também inclui os arquivos de imagem. O navegador de conferência bloqueou a abertura de arquivos locais; o acabamento visual e a paginação final precisam ser conferidos na impressão do navegador do usuário.

O contêiner Docker/PostgreSQL não foi executado no ambiente de criação, que não dispõe de Docker. O esquema PostgreSQL foi gerado pelo provedor Npgsql. O usuário já confirmou os fluxos da versão anterior no Docker/PostgreSQL local. Esta atualização foi compilada e testada aqui com SQLite/SMTP auxiliar; repita os novos fluxos no seu Docker.

Para repetir os testes contra o Docker (criam registros fictícios):

```powershell
python tests/smoke.py
```

O teste de recuperação aguarda o intervalo de envio de e-mails no modo Docker. Para testar sem Docker, com .NET SDK 10 e Python instalados, compile e use o modo auxiliar isolado:

```powershell
dotnet build src/Talume.Web
python tests/smoke.py --local dotnet
```

Esse comando inicia uma aplicação temporária com SQLite, cria uma caixa SMTP de teste, verifica os fluxos e encerra o processo. Não altera o PostgreSQL.

## Publicar no Railway

A configuração está preparada, mas nenhuma publicação no Railway foi realizada. O repositório escolhido para o projeto é `C4rlossz/Talume`, com visibilidade privada.

1. Use o repositório privado `C4rlossz/Talume` como origem e autorize o acesso do Railway quando configurar a publicação. Mantenha arquivos `.env` e bancos locais fora do Git.
2. Crie um serviço PostgreSQL no Railway e importe `docs/schema-postgres.sql` em um banco vazio, uma única vez.
3. Crie o serviço da aplicação usando o Dockerfile deste repositório.
4. Configure as variáveis abaixo no serviço da aplicação.
5. Gere o domínio, confira `/health` e teste convites com o envio real configurado. Use a integração Resend HTTPS e a lista de cadastros autorizados descritas em [Resend e Railway](docs/RESEND-RAILWAY.md).

Variáveis essenciais:

- `ASPNETCORE_ENVIRONMENT=Production`
- `ConnectionStrings__Default=Host=<host>;Port=5432;Database=<banco>;Username=<usuario>;Password=<senha>` usando os dados do serviço PostgreSQL. Npgsql espera essa connection string; não cole uma URL `postgresql://` sem convertê-la.
- `Database__Provider=Postgres`
- `Database__Initialize=false` (o esquema já deve estar importado)
- `Demo__Seed=false`
- `AllowedHosts=<dominio-gerado>`
- `App__BaseUrl=https://<dominio-gerado>`
- `Mail__Provider=Resend`, `Resend__ApiKey` e `Mail__From`, descritos acima.
- `Registration__AllowPublicFreelancers=false` e `Registration__AllowedEmails` com seus e-mails autorizados.
- `DataProtection__Path=/app/keys`, com armazenamento persistente apropriado para manter as chaves. Sem persistência, republicações podem invalidar sessões. Se usar volume, assegure permissão de escrita ao usuário `app`.

O aplicativo escuta na porta 8080. Configure essa porta como destino do domínio. Mantenha uma réplica nesta versão inicial. Não publique o Compose de desenvolvimento como produção: ele ativa exemplos e e-mails locais.

## O que falta para o primeiro uso com clientes reais

A base de banco e autenticação está implementada. A prévia HTML não usa essa base e perde as alterações ao recarregar. Para persistir, execute a aplicação completa com Docker/PostgreSQL.

Para um piloto com 1–3 desenvolvedores e cerca de 50 clientes:

- Validar a primeira execução no Docker/PostgreSQL, incluindo reiniciar e conferir os dados persistidos. O ambiente de criação não tem Docker; a validação automatizada usou SQLite auxiliar.
- Configurar hospedagem, HTTPS, domínio/base URL e remetente Resend real; testar convite, confirmação e recuperação em uma caixa real.
- Configurar backup e testar a restauração do PostgreSQL; definir rotina de atualização do esquema e acompanhar erros do serviço.
- Usar banco de produção sem contas fictícias, conferir permissões e estabelecer como atender solicitações de correção/exclusão de dados de clientes.
- Testar os fluxos de ponta a ponta com algumas contas antes de convidar todos. Ainda não foi realizado teste de carga para prometer capacidade ou desempenho.

Cada desenvolvedor tem seu espaço isolado. Uma equipe de vários desenvolvedores gerenciando os mesmos projetos ainda não está implementada. WhatsApp automático, cobrança online, upload de arquivos e aceite pelo cliente são evoluções opcionais; o acompanhamento básico já existe.

## Próximas evoluções

- Migrações completas versionadas de banco. A inicialização local usa `EnsureCreated` e atualizações aditivas específicas para perfil e logos; mudanças futuras em outras tabelas ainda exigem scripts ou migrações próprios.
- Upload privado de arquivos (a primeira versão compartilha links HTTPS).
- Notificações automáticas de etapas por e-mail e WhatsApp oficial.
- Aprovação pelo cliente, comentários e pedidos de revisão, se forem adicionados ao escopo.
- Pagamentos online, assinatura e emissão fiscal são integrações futuras, não funcionalidades desta versão.

Veja `docs/GUIA-DE-ESTUDO.md` para entender o código e os próximos exercícios.
