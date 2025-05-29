using Cashflow.Integration.Tests.Entities;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Shouldly;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace Cashflow.Integration.Tests.Tests.Controller
{
    public class CashflowFullStackTests : IAsyncLifetime
    {
        private INetwork _network = null!;
        private IContainer _pgContainer = null!;
        private IContainer _redisContainer = null!;
        private IContainer _rabbitContainer = null!;
        private IContainer _migrationsContainer = null!;
        private IContainer _operationsApi = null!;
        private IContainer _reportingApi = null!;
        private IContainer _worker = null!;

        // Hostnames únicos
        private const string HostNamePostgres = "test-postgres";
        private const string HostNameRabbit = "test-rabbit";
        private const string HostNameRedis = "test-redis";
        private const string HostNameMigrations = "test-migrations";
        private const string HostNameOpsApi = "test-operationsapi";
        private const string HostNameReportingApi = "test-reportingapi";
        private const string HostNameWorker = "test-worker";
        private const string DatabaseName = "cashflowdbtest";

        public async Task InitializeAsync()
        {

            // Cria rede customizada
            _network = new NetworkBuilder()
                 .WithName($"cashflow-test-network-integration_{Guid.NewGuid()}")
                 .Build();
            await _network.CreateAsync();


            _pgContainer = new ContainerBuilder()
                .WithImage("postgres:16")
                .WithName(HostNamePostgres)
                .WithHostname(HostNamePostgres)
                .WithNetwork(_network)
                .WithNetworkAliases(HostNamePostgres)
                .WithEnvironment("POSTGRES_DB", DatabaseName)
                .WithEnvironment("POSTGRES_USER", "postgres")
                .WithEnvironment("POSTGRES_PASSWORD", "postgres")
                .WithPortBinding(25431, 5432)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5432))
                .Build();

            await _pgContainer.StartAsync();

            var pgDockerHost = "test-postgres";
            var pgDockerPort = 5432;
            var dbName = "cashflowdbtest";
            var dbUser = "postgres";
            var dbPass = "postgres";

            // Para containers
            var pgConnectionString = $"Host={pgDockerHost};Port={pgDockerPort};Database={dbName};Username={dbUser};Password={dbPass}";

            // Migrations
            _migrationsContainer = new ContainerBuilder()
                .WithImage("cashflow-migrations:latest")
                .WithName(HostNameMigrations)
                .WithHostname(HostNameMigrations)
                .WithNetwork(_network)
                .WithNetworkAliases(HostNameMigrations)
                .WithEnvironment("ConnectionStrings__Postgres", pgConnectionString)
                .DependsOn(_pgContainer)
                .Build();

            await _migrationsContainer.StartAsync();


            // Redis (porta externa 26379)
            _redisContainer = new ContainerBuilder()
                .WithImage("redis:7")
                .WithName(HostNameRedis)
                .WithHostname(HostNameRedis)
                .WithNetwork(_network)
                .WithNetworkAliases(HostNameRedis)
                .WithPortBinding(26379, 6379)
                .Build();
            await _redisContainer.StartAsync();

            // RabbitMQ (porta externa 25672, mgmt 15673)
            _rabbitContainer = new ContainerBuilder()
                .WithImage("rabbitmq:3-management")
                .WithName(HostNameRabbit)
                .WithHostname(HostNameRabbit)
                .WithNetwork(_network)
                .WithNetworkAliases(HostNameRabbit)
                .WithEnvironment("RABBITMQ_DEFAULT_USER", "guest")
                .WithEnvironment("RABBITMQ_DEFAULT_PASS", "guest")
                .WithPortBinding(25672, 5672)
                .WithPortBinding(15673, 15672)
                .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(5672))
                .Build();
            await _rabbitContainer.StartAsync();

            //await Task.sle(5000);

            var redisHost = HostNameRedis;
            var redisPort = 6379;
            var rabbitHost = HostNameRabbit;
            var rabbitPort = 5672;

            // Operations API (porta externa 28090)
            _operationsApi = new ContainerBuilder()
                .WithImage("cashflowoperationsapi:latest")
                .WithName(HostNameOpsApi)
                .WithHostname(HostNameOpsApi)
                .WithNetwork(_network)
                .WithNetworkAliases(HostNameOpsApi)
                .WithPortBinding(28090, 8090)
                .WithEnvironment("ASPNETCORE_URLS", "http://+:8090")
                .WithEnvironment("Rabbit__Host", rabbitHost.ToString())
                .WithEnvironment("Rabbit__Port", rabbitPort.ToString())
                .WithEnvironment("Rabbit__UserName", "guest")
                .WithEnvironment("Rabbit__Password", "guest")
                .WithEnvironment("Redis__Host", redisHost)
                .WithEnvironment("Redis__Port", redisPort.ToString())
                .WithEnvironment("ConnectionStrings__Postgres", pgConnectionString)
                .DependsOn(_rabbitContainer)
                .DependsOn(_pgContainer)
                .DependsOn(_redisContainer)
                .Build();
            await _operationsApi.StartAsync();

            // Reporting API (porta externa 28092)
            _reportingApi = new ContainerBuilder()
                .WithImage("cashflowreportingapi:latest")
                .WithName(HostNameReportingApi)
                .WithHostname(HostNameReportingApi)
                .WithNetwork(_network)
                .WithNetworkAliases(HostNameReportingApi)
                .WithPortBinding(28092, 8092)
                .WithEnvironment("ASPNETCORE_URLS", "http://+:8092")
                .WithEnvironment("Redis__Host", redisHost)
                .WithEnvironment("Redis__Port", redisPort.ToString())
                .WithEnvironment("ConnectionStrings__Postgres", pgConnectionString)
                .DependsOn(_pgContainer)
                .DependsOn(_redisContainer)
                .Build();
            await _reportingApi.StartAsync();

            // Worker
            _worker = new ContainerBuilder()
                .WithImage("cashflowconsolidationworker:latest")
                .WithName(HostNameWorker)
                .WithHostname(HostNameWorker)
                .WithNetwork(_network)
                .WithNetworkAliases(HostNameWorker)
                .WithEnvironment("Rabbit__Host", rabbitHost)
                .WithEnvironment("Rabbit__Port", rabbitPort.ToString())
                .WithEnvironment("Rabbit__UserName", "guest")
                .WithEnvironment("Rabbit__Password", "guest")
                .WithEnvironment("ConnectionStrings__Postgres", pgConnectionString)
                .DependsOn(_pgContainer)
                .DependsOn(_rabbitContainer)
                .Build();
            await _worker.StartAsync();
        }

        public async Task DisposeAsync()
        {
            await _worker.StopAsync();
            await _reportingApi.StopAsync();
            await _operationsApi.StopAsync();
            await _rabbitContainer.StopAsync();
            await _redisContainer.StopAsync();
            await _pgContainer.StopAsync();
            await _network.DeleteAsync();
            await _migrationsContainer.StopAsync();


            await _worker.DisposeAsync();
            await _reportingApi.DisposeAsync();
            await _operationsApi.DisposeAsync();
            await _rabbitContainer.DisposeAsync();
            await _redisContainer.DisposeAsync();
            await _pgContainer.DisposeAsync();
            await _migrationsContainer.DisposeAsync();
        }

        [Fact]
        public async Task Should_Create_Transaction_And_Retrieve_Balance()
        {
            var opsApiHost = _operationsApi.Hostname;
            var opsApiPort = _operationsApi.GetMappedPublicPort(8090);
            var opsApiUrl = $"http://{opsApiHost}:{opsApiPort}";

            using var client = new HttpClient { BaseAddress = new Uri(opsApiUrl) };
            var responseToken = await client.GetAsync("/api/Token/Generate");
            var tokenResult = await responseToken.Content.ReadFromJsonAsync<TokenResponse>();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult!.Token);

            var debits = new[]
            {
                new { Amount = 4000m, IdempotencyKey = Guid.NewGuid(), Type = 1 },
                new { Amount = 600m,  IdempotencyKey = Guid.NewGuid(), Type = 1 }
            };

            var credits = new[]
            {
                new { Amount = 1000m, IdempotencyKey = Guid.NewGuid(), Type = 2 },
                new { Amount = 500m,  IdempotencyKey = Guid.NewGuid(), Type = 2 }
            };

            foreach (var debit in debits)
            {
                var resp = await client.PostAsJsonAsync("/api/transactions", debit);
                resp.EnsureSuccessStatusCode();
            }

            foreach (var credit in credits)
            {
                var resp = await client.PostAsJsonAsync("/api/transactions", credit);
                resp.EnsureSuccessStatusCode();
            }

            var repApiHost = _reportingApi.Hostname;
            var repApiPort = _reportingApi.GetMappedPublicPort(8092);
            var repApiUrl = $"http://{repApiHost}:{repApiPort}";

            using var reportingClient = new HttpClient { BaseAddress = new Uri(repApiUrl) };

            var expectedDebit = debits.Sum(x => x.Amount);
            var expectedCredit = credits.Sum(x => x.Amount);

            var balanceResponse = await WaitForBalanceAsync(
                reportingClient,
                $"/transactions/balance/{DateTime.UtcNow:yyyy-MM-dd}",
                expectedDebit,
                expectedCredit
            );

            balanceResponse.ShouldNotBeNull("O saldo esperado não foi processado dentro do timeout.");
            balanceResponse!.Totals.Debit.ShouldBe(expectedDebit);
            balanceResponse.Totals.Credit.ShouldBe(expectedCredit);
        }


        [Fact]
        public async Task Should_Block_Same_Request_Idpotency()
        {
            var opsApiHost = _operationsApi.Hostname;
            var opsApiPort = _operationsApi.GetMappedPublicPort(8090);
            var opsApiUrl = $"http://{opsApiHost}:{opsApiPort}";

            using var client = new HttpClient { BaseAddress = new Uri(opsApiUrl) };
            var responseToken = await client.GetAsync("/api/Token/Generate");
            var tokenResult = await responseToken.Content.ReadFromJsonAsync<TokenResponse>();

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult!.Token);

            var request = new
            {
                Amount = 123.45M,
                IdempotencyKey = Guid.NewGuid(),
                Type = 1
            };

            var response = await client.PostAsJsonAsync("/api/transactions", request);

            await Task.Delay(1500);

            var responseError = await client.PostAsJsonAsync("/api/transactions", request);

            var repApiHost = _reportingApi.Hostname;
            var repApiPort = _reportingApi.GetMappedPublicPort(8092);

            responseToken.EnsureSuccessStatusCode();
            response.EnsureSuccessStatusCode();
            responseError.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        }

        private async Task<BalanceResponse?> WaitForBalanceAsync(HttpClient client, string url, decimal expectedDebit, decimal expectedCredit, int timeoutSeconds = 10)
        {
            var sw = Stopwatch.StartNew();
            while (sw.Elapsed.TotalSeconds < timeoutSeconds)
            {
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    var balance = await response.Content.ReadAsStringAsync();
                    if (!string.IsNullOrWhiteSpace(balance))
                    {
                        var balanceResponse = JsonSerializer.Deserialize<BalanceResponse>(balance, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        if (balanceResponse != null
                            && balanceResponse.Totals.Debit == expectedDebit
                            && balanceResponse.Totals.Credit == expectedCredit)
                        {
                            return balanceResponse;
                        }
                    }
                }
                await Task.Delay(500);
            }
            return null;
        }
    }
}
