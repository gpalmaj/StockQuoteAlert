Esse arquivo é um registro da minha tomada de decisao e planejamento para a implementacao do projeto. 

# Requisitos:

- O sistema desenvolvido deve avisar, via e-mail, caso a cotação de um ativo da B3 caia mais do que certo nível, ou suba acima de outro.
- O programa deve ser uma aplicação de console (não há necessidade de interface gráfica).
Ele deve ser chamado via linha de comando com 3 parâmetros. 

#### Parametros de linha de comando:
1. Ativo
2. Preco para venda
3. Preco para compra

## Arquivo de configuracao:
Contem email alvo e config do SMTP. Obrigatoriamente segue o formato especificado pelas classes definidas ./Models/AlertParticipants.cs

---

### Escolha de API para precos
Pesquisas me deram diferentes possibilidades para APIs de rastreamento de preco de acoes. A mais acessivel completa para acoes B3 foi a brapi.
### Escolha de Biblioteca para e-mail
O mailkit é a escolha óbvia para o projeto simples.

---

# Implementacao MVP
Fazer, para uma primeira etapa, uma versao simples que cupmre minimamente os requisitos. 
Divido o MVP tres modulos principais:
- Consulta: Consulta da API 
- Notificacao: Envio de email
- Main: Orquestração dos processos e implementação da lógica 

O projeto precisa de modulos auxiliares para poder implementar as funcionalidades principais. 
  
### Consulta [./Services/QuoteService.cs] (./Services/ClientSetup como auxiliar)
Responsável pela aquisicao das informacoes úteis para o cumprimento dos requisitos.

__Funcionalidade principal__: Consulta a API pelo preco de uma acao atraves da brapi, com uma consulta http. 

### Notificacao [./Services/EmailService]
Responsavel por gerenciar os processos e implementacao do SMTP para o envio de emails
A classe email sevice 
- inicializa o serviço, com um recebedor e um "enviador" SMTP.
- estrutura e envia a mensagem usando a biblioteca Mailkit.

### Main [./Program.cs]
Ponto de entrada, responsavel por implementar as funcionalidades definidas nos demais modulos. 
É necessario definir a frequencia de polling da API (toda a capacidade que a API permite?). 
- Frequencia de *uma query por minuto* parace ok

__Logica__:
- Cada x segundos, pega preco da api
- compara se estava dentro do intervalo
- se estava e nao está, mandar o e-mail correspondente
- se continua, faz nada.


O enunciado do desafio cita:\
"Toda vez que o preço for maior que linha-azul, um e-mail deve ser disparado aconselhando a venda.

Toda vez que o preço for menor que linha-vermelha, um e-mail deve ser disparado aconselhando a compra."

Apesar do fraseamento em inclinar a enviar emails toda vez que a query encontrar um valor fora dos limites, um email por minuto em caso da acao se manter na zona em questao nao é razoavel, entao foi implementado o padrao edge-triggered.

---

## Refinando
Antes do envio, vou "arredondar" as operacoes do programa, para evitar crashes e falhas improdutivas.

__Main__: Para a main, o parsing de argumentos e possiveis falhas na API sao o principal ponto a tratar. O try-catch do loop precisa tratar de casos especificos.
__EmailService__: Possiveis erros na leitura do arquivo e processamento de credenciais

---

### Entrega
Para a entrega, substituí o arquivo appsettings com um exemplo, para nao vazar nenhuma chave minha (as do historico estao invalidadas.)
Não commitarei binários, mas ele pode ser gerado pelo seguinte comando:

```
dotnet publish -c Release -r <rid> --self-contained -p:PublishSingleFile=true
```

### Testes
Foram realizados durante o pregão, para gatilhos superiores e inferiores.
A API funciona normalmente nos demais horarios do dia.

---
# Referências
- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/language-specification/standard-library
- https://dotnetfoundation.org/news-events/detail/mailkit-working-with-emails
- https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/deserialization
- https://brapi.dev/docs
- https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-messages/nullable-warnings#possible-null-assigned-to-a-nonnullable-reference

---
# Uso de LLMs
O modelo Opus 4.6 foi consultado pelo aplicativo Claude.ai, apenas para elucidacao da sintaxe C#.
Claude Code com Opus 4.6 foi usado para gerar o README.