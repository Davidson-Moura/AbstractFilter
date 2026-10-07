using AbstractFilter;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Diagnostics;

namespace Tests
{
    public class Produto
    {
        public ObjectId Id { get; set; }
        public string Nome { get; set; }
        public decimal Preco { get; set; }
        public string Categoria { get; set; }
        public List<ProdutoLine> Linhas { get; set; } = new List<ProdutoLine>();
    }
    public class ProdutoLine
    {
        public string Name { get; set; }
    }
    public class ProdutoDto
    {
        public string NomeDoProduto { get; set; }
        public decimal Valor { get; set; }
    }
    public class MongoTestFixture
    {
        public IMongoClient Client { get; }
        public string DatabaseName => "TesteDb";
        public string CollectionName => "Produtos";

        public MongoTestFixture()
        {
            var connectionString = "mongodb://admin:123456@localhost:27017/?authSource=admin";
            Client = new MongoClient(connectionString);

            // Executa a inserção dos dados iniciais de forma síncrona no arranque da fixture
            InitializeDataAsync().GetAwaiter().GetResult();
        }

        private async Task InitializeDataAsync()
        {
            var database = Client.GetDatabase(DatabaseName);
            await database.DropCollectionAsync(CollectionName);

            var collection = database.GetCollection<Produto>(CollectionName);

            var produtos = new List<Produto>
            {
                new Produto
                {
                    Id = ObjectId.Parse("507f1f77bcf86cd799439011"),
                    Nome = "Teclado Mecânico",
                    Preco = 250.00m,
                    Categoria = "Periféricos",
                    Linhas = new List<ProdutoLine> { new ProdutoLine { Name = "A" }, new ProdutoLine { Name = "B" } }
                },
                new Produto
                {
                    Id = ObjectId.Parse("507f1f77bcf86cd799439012"),
                    Nome = "Rato Gamer",
                    Preco = 120.00m,
                    Categoria = "Periféricos",
                    Linhas = new List<ProdutoLine> { new ProdutoLine { Name = "A" } }
                },
                new Produto
                {
                    Id = ObjectId.Parse("507f1f77bcf86cd799439013"),
                    Nome = "Monitor 24\"",
                    Preco = 900.00m,
                    Categoria = "Monitores",
                    Linhas = new List<ProdutoLine> { new ProdutoLine { Name = "C" } }
                },
                new Produto
                {
                    Id = ObjectId.Parse("507f1f77bcf86cd799439014"),
                    Nome = "Cadeira Gamer",
                    Preco = 1200.00m,
                    Categoria = "Mobiliário",
                    Linhas = new List<ProdutoLine> { new ProdutoLine { Name = "B" } }
                }
            };

            await collection.InsertManyAsync(produtos);
        }
    }
    public class MongoFilterTest : IClassFixture<MongoTestFixture>
    {
        private readonly MongoTestFixture _fixture;

        public MongoFilterTest(MongoTestFixture fixture)
        {
            _fixture = fixture;
        }

        private IMongoCollection<Produto> GetCollection()
        {
            return _fixture.Client.GetDatabase(_fixture.DatabaseName).GetCollection<Produto>(_fixture.CollectionName);
        }

        [Fact]
        public async Task TestEqFilter()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<Produto>();
            var filter = builder.Eq(p => p.Categoria, "Periféricos");

            var resultado = await collection.Find(Builders<Produto>.Filter.Where(filter.ToExpression())).ToListAsync();
            Assert.Equal(2, resultado.Count);
        }

        [Fact]
        public async Task TestNeFilter()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<Produto>();
            var filter = builder.Ne(p => p.Categoria, "Periféricos");

            var resultado = await collection.Find(Builders<Produto>.Filter.Where(filter.ToExpression())).ToListAsync();
            Assert.Equal(2, resultado.Count);
        }

        [Fact]
        public async Task TestGteAndLteFilters()
        {
            var stopwatch = Stopwatch.StartNew();

            var collection = GetCollection();
            var builder = new ABFilterBuilder<Produto>();
            var filter = builder.And(
                builder.Gte(p => p.Preco, 200.00m),
                builder.Lte(p => p.Preco, 1000.00m)
            );

            var exp = Builders<Produto>.Filter.Where(filter.ToExpression());

            var filterTime = stopwatch.ElapsedMilliseconds;
            stopwatch.Restart();

            var resultado = await collection.Find(exp).ToListAsync();

            var dbTime = stopwatch.ElapsedMilliseconds;

            Assert.Equal(2, resultado.Count);
        }

        [Fact]
        public async Task TestInAndNotInFilters()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<Produto>();

            var filterIn = builder.In(p => p.Categoria, new[] { "Monitores", "Mobiliário" });
            var resultadoIn = await collection.Find(Builders<Produto>.Filter.Where(filterIn.ToExpression())).ToListAsync();
            Assert.Equal(2, resultadoIn.Count);

            var filterNotIn = builder.NotIn(p => p.Categoria, new[] { "Periféricos" });
            var resultadoNotIn = await collection.Find(Builders<Produto>.Filter.Where(filterNotIn.ToExpression())).ToListAsync();
            Assert.Equal(2, resultadoNotIn.Count);
        }

        [Fact]
        public async Task TestRegexFilter()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<Produto>();
            var filter = builder.Regex(p => p.Nome, "Gamer");

            var resultado = await collection.Find(Builders<Produto>.Filter.Where(filter.ToExpression())).ToListAsync();
            Assert.Equal(2, resultado.Count);
        }

        [Fact]
        public async Task TestOrFilter()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<Produto>();
            var filter = builder.Or(
                builder.Eq(p => p.Categoria, "Monitores"),
                builder.Eq(p => p.Preco, 1200.00m)
            );

            var resultado = await collection.Find(Builders<Produto>.Filter.Where(filter.ToExpression())).ToListAsync();
            Assert.Equal(2, resultado.Count);
        }

        [Fact]
        public async Task TestElemMatchAndAnyInFilters()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<Produto>();

            var filterElem = builder.ElemMatch<Produto, ProdutoLine>(p => p.Linhas, l => l.Name == "C");
            var resultadoElem = await collection.Find(Builders<Produto>.Filter.Where(filterElem.ToExpression())).ToListAsync();
            Assert.Single(resultadoElem);

            var filterAnyIn = builder.AnyIn<Produto, string>(p => p.Linhas.Select(l => l.Name), new[] { "C" });
            var resultadoAnyIn = await collection.Find(Builders<Produto>.Filter.Where(filterAnyIn.ToExpression())).ToListAsync();
            Assert.Single(resultadoAnyIn);
        }

        [Fact]
        public async Task TestSortByPriceAscending()
        {
            var collection = GetCollection();

            // Constrói a regra de ordenação por preço crescente
            var sortBuilder = new ABFSortBuilder<Produto>();
            var sortDef = sortBuilder.OrderBy(p => p.Preco).Build();

            // Aplica a ordenação utilizando o IQueryable da coleção
            var resultado = collection.AsQueryable().ApplySort(sortDef).ToList();

            // Valida se a quantidade está correta e a ordem (Rato: 120 -> Teclado: 250 -> Monitor: 900 -> Cadeira: 1200)
            Assert.Equal(4, resultado.Count);
            Assert.Equal("Rato Gamer", resultado[0].Nome);
            Assert.Equal("Teclado Mecânico", resultado[1].Nome);
            Assert.Equal("Monitor 24\"", resultado[2].Nome);
            Assert.Equal("Cadeira Gamer", resultado[3].Nome);
        }

        [Fact]
        public async Task TestSortByPriceDescending()
        {
            var collection = GetCollection();

            // Constrói a regra de ordenação por preço decrescente
            var sortBuilder = new ABFSortBuilder<Produto>();
            var sortDef = sortBuilder.OrderByDescending(p => p.Preco).Build();

            var resultado = collection.AsQueryable().ApplySort(sortDef).ToList();

            // Valida a ordem inversa (Cadeira: 1200 -> Monitor: 900 -> Teclado: 250 -> Rato: 120)
            Assert.Equal(4, resultado.Count);
            Assert.Equal("Cadeira Gamer", resultado[0].Nome);
            Assert.Equal("Monitor 24\"", resultado[1].Nome);
            Assert.Equal("Teclado Mecânico", resultado[2].Nome);
            Assert.Equal("Rato Gamer", resultado[3].Nome);
        }

        [Fact]
        public async Task TestMultiLevelSort()
        {
            var collection = GetCollection();

            // Constrói uma ordenação composta: Categoria (crescente) e depois Preço (decrescente para desempate)
            var sortBuilder = new ABFSortBuilder<Produto>();
            var sortDef = sortBuilder
                .OrderBy(p => p.Categoria)
                .ThenByDescending(p => p.Preco)
                .Build();

            var resultado = collection.AsQueryable().ApplySort(sortDef).ToList();

            Assert.Equal(4, resultado.Count);

            // 1. "Mobiliário" (Cadeira Gamer - 1200)
            Assert.Equal("Cadeira Gamer", resultado[0].Nome);

            // 2. "Monitores" (Monitor 24" - 900)
            Assert.Equal("Monitor 24\"", resultado[1].Nome);

            // 3. "Periféricos" -> Temos dois produtos (Teclado 250 e Rato 120). 
            // Como o ThenByDescending foi aplicado no preço, o Teclado (250) deve vir antes do Rato (120).
            Assert.Equal("Teclado Mecânico", resultado[2].Nome);
            Assert.Equal("Rato Gamer", resultado[3].Nome);
        }

        [Fact]
        public async Task TestPagination_SkipAndTake()
        {
            var collection = GetCollection();

            // Criamos o finder configurado para ordenar por preço, saltar 1 e pegar 2 registos
            var finder = new ABFinder<Produto>
            {
                Sort = new ABFSortBuilder<Produto>().OrderBy(p => p.Preco).Build(),
                Skip = 1,
                Take = 2
            };

            var resultado = collection.AsQueryable().ApplyFinder(finder).ToList();

            // Validação: 
            // Com ordenação por preço, a lista original seria: 
            // 1. Rato Gamer (120)
            // 2. Teclado Mecânico (250)
            // 3. Monitor 24" (900)
            // 4. Cadeira Gamer (1200)
            // Ao aplicar Skip = 1 e Take = 2, devemos obter exatamente o Teclado e o Monitor.

            Assert.Equal(2, resultado.Count);
            Assert.Equal("Teclado Mecânico", resultado[0].Nome);
            Assert.Equal("Monitor 24\"", resultado[1].Nome);
        }

        [Fact]
        public async Task TestProjection()
        {
            var collection = GetCollection();

            // Criamos o finder tipado para transformar Produto em ProdutoDto
            var finder = new ABFinder<Produto, ProdutoDto>
            {
                Filter = new ABFExpressionFilter<Produto>(p => p.Categoria == "Periféricos"),
                Projection = p => new ProdutoDto
                {
                    NomeDoProduto = p.Nome,
                    Valor = p.Preco
                }
            };

            var resultado = collection.AsQueryable().ApplyFinder(finder).ToList();

            // Validação: Deve encontrar 2 produtos periféricos convertidos para o DTO customizado
            Assert.Equal(2, resultado.Count);

            // Verifica se os campos foram projetados corretamente
            Assert.All(resultado, item =>
            {
                Assert.False(string.IsNullOrEmpty(item.NomeDoProduto));
                Assert.True(item.Valor > 0);
            });

            Assert.Contains(resultado, r => r.NomeDoProduto == "Teclado Mecânico" && r.Valor == 250.00m);
            Assert.Contains(resultado, r => r.NomeDoProduto == "Rato Gamer" && r.Valor == 120.00m);
        }
    }
}
