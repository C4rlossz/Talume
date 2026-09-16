# Estudando o Talume

## 1. Faça o caminho completo antes de estudar os arquivos

Entre como freelancer, cadastre um cliente e um serviço, crie um orçamento e transforme-o em projeto. Entre como cliente em outra janela. Compare os dados recebidos pelos dois perfis.

Pergunta: por que esconder um botão não basta para proteger uma informação?

## 2. Leia o modelo de dados

Comece em `Models/Entities.cs` e em `Data/AppDbContext.cs`.

| Entidade | Responsabilidade |
|---|---|
| AppUser | Conta, nome e tipo de usuário |
| Client | Cliente pertencente a um freelancer; pode ser vinculado a uma conta |
| CatalogService | Serviço reutilizável com preço base e prazo estimado |
| Quote / QuoteLine | Orçamento e cópias dos valores negociados |
| Project | Trabalho contratado, etapa, prazo e valor |
| ProjectTask | Tarefa interna |
| ProjectUpdate | Histórico, com visibilidade interna ou compartilhada |
| Delivery | Link HTTPS de uma entrega |
| Installment | Parcela prevista e data do recebimento |
| Invitation | Convite com token, validade e registro de uso |
| EmailChallenge | Código de confirmação ou recuperação, tentativas e validade |

`OwnerId` identifica o freelancer dono do registro. `UserId` de Client identifica a conta que pode acompanhar os projetos daquele cliente. Os campos têm funções diferentes.

Exercício: explique por que QuoteLine armazena o preço em vez de buscar sempre o valor atual do catálogo.

## 3. Siga uma requisição

Abra `wwwroot/app.js` e procure `api('/projects','POST'`. O navegador envia JSON para o servidor, com cookie de sessão e token CSRF. Em `BusinessEndpoints.cs`, o backend:

1. Exige o perfil Freelancer.
2. Valida os campos.
3. Confirma que o cliente pertence ao usuário autenticado.
4. Cria a entidade Project.
5. Salva no banco usando EF Core.
6. Retorna o identificador do novo projeto.

Exercício: adicione um limite menor para o título e confirme que o backend rejeita valores acima desse limite, mesmo que o JavaScript seja alterado.

## 4. Entenda autenticação e autorização

Autenticação responde quem está acessando. Autorização responde o que essa pessoa pode consultar ou alterar.

Identity cuida das senhas e sessões. `Services/Access.cs` cuida dos limites das consultas. Os endpoints de escrita exigem o papel Freelancer e conferem a propriedade dos registros.

Exercício: faça login como cliente e tente consultar um ID de projeto de outro cliente. O resultado correto é 404 sem dados do projeto.

## 5. Estude confirmação e recuperação

Abra `AuthEndpoints.cs`. A aplicação gera um código aleatório, guarda seu hash e envia o código por SMTP. A confirmação ocorre dentro de transação. O cliente só é vinculado após provar que recebeu o código. Na confirmação, define novamente sua senha: isso evita preservar a senha de um cadastro pendente criado anteriormente por outra pessoa.

O convite e o código são credenciais diferentes. O convite indica qual cliente está sendo vinculado; o código comprova acesso ao e-mail.

Exercício: tente usar o mesmo código duas vezes e leia a resposta da API.

## 6. Entenda os valores e as transações

Valores usam `decimal`, apropriado para representar números decimais de dinheiro. Um índice único sobre Project.QuoteId reforça que cada orçamento origine no máximo um projeto. Transações serializáveis protegem conversão, confirmação e soma de parcelas de alterações concorrentes.

Exercício: cadastre uma parcela maior que o valor do projeto. Depois explique por que a validação precisa ficar no servidor.

## 7. Conheça os três serviços Docker

- `app`: compila e executa C#.
- `db`: armazena os registros PostgreSQL em volume persistente.
- `mailpit`: recebe os e-mails locais para inspeção.

O navegador usa localhost:8080. Dentro dos contêineres, a aplicação usa `db:5432` e `mailpit:1025`. `localhost` dentro de um contêiner se refere ao próprio contêiner.

## 8. Evolua em pequenas entregas

1. Acrescente uma categoria ao serviço, com validação e campo na tela.
2. Mostre um indicador de prazo vencido na página de detalhes.
3. Escreva uma migration para a nova categoria antes de reutilizar um banco existente.
4. Adicione comentários do cliente com regras explícitas de autorização.
5. Só depois integre notificações externas.

Para o portfólio, descreva o que você realmente estudou, implementou e modificou. Uma apresentação pode mostrar: problema, demonstração de dois perfis, banco de dados, uma regra de segurança, teste e aprendizado. Não apresente a prévia HTML como se fosse o backend .NET.
