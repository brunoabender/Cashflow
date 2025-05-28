# Decisões Arquiteturais e Trade-offs

## Microserviços x Monolito

Adotei uma arquitetura baseada em microserviços para garantir o desacoplamento dos domínios de negócio, permitir o deploy independente de cada componente e possibilitar a escalabilidade individualizada das APIs e do Worker conforme demanda. Avaliei utilizar um monolito, que poderia ser mais simples para desenvolvimento e deploy inicial, porém não atenderia à necessidade de desacoplamento e tolerância a falhas entre domínios — por exemplo, a indisponibilidade do consolidado não pode afetar o serviço de lançamentos. O microserviço impõe complexidade operacional (deploy, mensageria, monitoramento), mas justifica-se pelo cenário de crescimento e requisitos de disponibilidade.

## RabbitMQ x Kafka

Para mensageria, utilizei o RabbitMQ por ser mais simples de operar, ter ótima integração com .NET e facilitar o padrão de filas com garantia de entrega (ack/nack) e Dead Letter Queue (DLQ) de forma nativa. Considerei usar Kafka, que é excelente para processamento de eventos em larga escala e retenção histórica, mas demanda maior complexidade operacional, necessidade de infraestrutura mais robusta e não entrega out-of-the-box os mesmos padrões de DLQ e gerenciamento por fila. Dado o perfil do problema (eventos transacionais, não analytics), RabbitMQ entrega a simplicidade, confiabilidade e robustez necessárias para o contexto.

## Redis como Cache x Outros Caches

Implementei o Redis como cache de alto desempenho para os saldos consolidados diários, atendendo ao requisito de suportar picos de acesso ao serviço de relatórios. Considerei caches embutidos ou soluções como Memcached, mas Redis oferece melhor suporte a estratégias de expiração, persistência opcional e fácil clusterização. O trade-off é a dependência de um serviço externo, porém o ganho em performance e facilidade de integração justificam a escolha.

## PostgreSQL x MongoDB

Escolhi PostgreSQL como banco principal devido à necessidade de transações ACID, integridade referencial, suporte a queries complexas e familiaridade do ecossistema com padrões bancários. Analisei o uso de MongoDB, que traria flexibilidade de schema e escalabilidade horizontal facilitada, mas para o caso de uso de lançamentos financeiros (onde atomicidade e consistência são críticos), PostgreSQL é mais adequado. O trade-off é menor flexibilidade de schema, porém o benefício de robustez e segurança transacional é essencial para o domínio financeiro.

## Dapper x ORMs Full-Stack (EF Core)

Utilizei o Dapper para acesso a dados devido à sua leveza, performance e controle total das queries SQL, algo importante para cenários de alta performance e fácil tuning de consultas. Considerei Entity Framework Core, que traria produtividade com LINQ e mapeamento automático, mas poderia incorrer em overheads desnecessários para um sistema crítico em desempenho e de modelo relacional simples. O trade-off é ter menos abstração e mais código SQL, mas com ganhos em performance e previsibilidade.

## Vertical Slice x Camadas Clássicas

Adotei o padrão Vertical Slice para evitar acoplamento excessivo entre camadas tradicionais (Controller, Service, Repository), tornando cada feature mais independente e fácil de testar. Em sistemas clássicos, a abordagem em camadas traz clareza, mas pode induzir dependências cruzadas, dificultando manutenção à medida que a aplicação cresce. O trade-off é sair do padrão mainstream para um padrão menos adotado, mas com ganhos significativos em modularidade e organização do código.

## Docker x Instalação Manual

Utilizo Docker e Docker Compose para orquestração e provisionamento dos ambientes, evitando problemas de "works on my machine" e simplificando a subida da stack completa em qualquer ambiente. O trade-off é exigir conhecimento prévio em containers para quem for manter ou evoluir o projeto, mas os benefícios em padronização e portabilidade superam esse custo inicial.

## Não Serverless/FaaS

Considerei arquiteturas serverless/FaaS (como Azure Functions ou AWS Lambda), mas rejeitei pela necessidade de controle fino sobre mensageria, conexões persistentes (RabbitMQ) e banco de dados transacional, além de não querer depender de vendor lock-in em cloud. Serverless facilitaria o scaling automático, mas a natureza transacional e o controle sobre resiliência justificam a escolha por serviços persistentes em containers.

## Segurança

Estruturei a solução para autenticação/autorização via JWT (não implementado nesta entrega). Avaliei outras formas (API Key, OAuth2 completo), mas JWT oferece bom equilíbrio entre segurança e facilidade de integração para sistemas desacoplados. O trade-off é a necessidade de infraestrutura de Identity Provider em ambientes mais avançados.

## Sobre Bibliotecas Comerciais

Não utilizei bibliotecas pagas ou de uso comercial no projeto. Todas as soluções e frameworks adotados são open source ou possuem licença gratuita para uso em projetos desse porte, visando evitar custos e dependências desnecessárias. Priorizo sempre alternativas robustas, mantidas pela comunidade e amplamente adotadas no mercado, garantindo sustentabilidade e facilidade de manutenção.

