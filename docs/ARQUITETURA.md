# Arquitetura da Solução - Cashflow

## 🏗️ Visão Geral

A solução Cashflow foi projetada para ser **modular, resiliente, escalável e segura**, com foco em controle de lançamentos financeiros e consolidação de saldos diários. A arquitetura segue princípios de microserviços desacoplados, mensageria assíncrona, cache distribuído e persistência robusta.

---

## 📦 Componentes da Solução

- **Cashflow.Operations.Api**  
  API responsável por registrar lançamentos (débito/crédito) e publicar eventos para a fila de processamento.

- **Cashflow.Consolidation.Worker**  
  Worker dedicado ao consumo dos eventos de lançamento, consolidação do saldo diário e atualização dos dados no banco.

- **Cashflow.Reporting.Api**  
  API para consulta dos saldos diários consolidados, com uso intensivo de cache para alta performance.

- **RabbitMQ**  
  Broker de mensageria que desacopla APIs do Worker, garante resiliência e possibilita retries e Dead Letter Queue (DLQ).

- **PostgreSQL**  
  Banco relacional para armazenamento persistente dos lançamentos e saldos consolidados.

- **Redis**  
  Cache de alta performance para otimizar consultas ao saldo consolidado, minimizando pressão sobre o banco de dados.

---

## 🔗 Diagrama de Componentes

```
Usuário
   |
   v
[Cashflow.Operations.Api] ---> (RabbitMQ) ---> [Cashflow.Consolidation.Worker] ---> [PostgreSQL]
   ^                                                                      |
   |                                                                      |
   +------------------ [Cashflow.Reporting.Api] <----- (Redis) <----------+
```

---

## 🔄 Fluxo Principal

1. **Lançamento:**  
   Usuário realiza lançamento (débito/crédito) via API.

2. **Publicação:**  
   API registra o lançamento e publica evento no RabbitMQ.

3. **Processamento:**  
   Worker consome evento, consolida saldo diário e persiste no PostgreSQL.

4. **Consulta:**  
   API de relatório consulta saldo diário (primeiro busca em cache Redis, depois no banco se necessário).

---

## 🛡️ Resiliência & Escalabilidade

- **Mensageria assíncrona (RabbitMQ):**  
  Garante desacoplamento entre serviços. Se o Worker ou Reporting cair, a API de operações segue funcionando normalmente.

- **Retry & DLQ:**  
  Mensagens não processadas podem ser reenviadas ou encaminhadas para fila de Dead Letter para análise posterior.

- **Cache Redis:**  
  Minimiza o impacto de picos de leitura no serviço de relatório. Em caso de falha do Redis, API ainda pode buscar dados no banco.

- **Escalabilidade horizontal:**  
  Todos os serviços são stateless e podem ser replicados conforme a demanda. RabbitMQ, Redis e Postgres também suportam clusterização.

---

## 🔒 Segurança

- **Autenticação & Autorização:**  
  APIs preparadas para integração com JWT/OAuth2 (não implementado nesta versão, mas fácil de plugar).

- **Criptografia:**  
  Comunicação interna pode ser protegida via TLS.

- **Idempotência:**  
  Garante que lançamentos duplicados não serão processados novamente, protegendo a consistência dos dados.

---

## 🏁 Requisitos Não Funcionais Atendidos

- **Alta disponibilidade:**  
  Serviços independentes, com possibilidade de múltiplas instâncias e failover.

- **Performance:**  
  Uso de Redis, consultas otimizadas e cache distribuído.

- **Confiabilidade:**  
  Persistência robusta em PostgreSQL e mensageria confiável.

- **Monitoramento e observabilidade:**  
  Preparado para logs estruturados, métricas e tracing (não incluso, mas pronto para expansão).

---

## 🔄 Possíveis Evoluções Futuras

- **Autenticação/autorização JWT integrada**
- **Tracing distribuído e métricas (Prometheus, Grafana)**
- **Health Checks robustos para readiness/liveness**
- **Gerenciamento de filas DLQ**
- **Orquestração Kubernetes com autoscaling**
- **Resiliência avançada: circuit breaker, fallback**

---

## 📝 Decisões Arquiteturais

- **Microserviços** para desacoplamento, resiliência e deploy independente.
- **RabbitMQ** como backbone para orquestração de eventos e desacoplamento.
- **Cache Redis** para garantir alta performance no relatório diário.
- **Persistência relacional (PostgreSQL)** para consistência e ACID.
- **Vertical Slice Architecture** para separação de responsabilidades e fácil manutenção.
- **Testes automatizados** cobrindo casos críticos.
- **Docker** e **Docker Compose** para fácil deploy e replicação.

---

## 📈 Métricas e SLA

- **Reporting API:** dimensionada para suportar picos de 50 req/s com <5% de perda.
- **Resiliência:** APIs nunca ficam indisponíveis mesmo que o Worker ou Reporting falhe.

---

## 🗂️ Referências

- Documentação detalhada de endpoints em cada projeto
- [README.md](../README.md)
- Diagramas em [docs/diagramas/](./diagramas/)

---
