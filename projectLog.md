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
Contem email alvo e config do SMTP

---

# Escolha de API para precos

Pesquisas me deram diferentes possibilidades para APIs de rastreamento de preco de acoes. A mais acessivel completa para acoes B3 foi a brapi.

# Escolha de Biblioteca para e-mail

O mailkit é a escolha óbvia para o projeto simples.




