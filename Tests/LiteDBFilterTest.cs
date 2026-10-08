using AbstractFilter;
using LiteDB;

namespace Tests
{
    public class LiteDbProduto
    {
        // No LiteDB, o Id pode ser ObjectId nativo do LiteDB ou Guid/Int
        public ObjectId Id { get; set; }
        public string Nome { get; set; }
        public decimal Preco { get; set; }
        public string Categoria { get; set; }
        public List<LiteDbProdutoLine> Linhas { get; set; } = new List<LiteDbProdutoLine>();
    }

    public class LiteDbProdutoLine
    {
        public string Name { get; set; }
    }

    public class LiteDbProdutoDto
    {
        public string NomeDoProduto { get; set; }
        public decimal Valor { get; set; }
    }

    // Fixture para inicializar a base de dados LiteDB em memória apenas uma vez
    public class LiteDbTestFixture
    {
        public LiteDatabase Database { get; }
        public string CollectionName => "Produtos";

        public LiteDbTestFixture()
        {
            // Utiliza uma base de dados em memória do LiteDB para testes rápidos e isolados
            Database = new LiteDatabase("Filename=:memory:");
            InitializeData();
        }

        private void InitializeData()
        {
            var collection = Database.GetCollection<LiteDbProduto>(CollectionName);

            // Garante que a coleção está limpa
            collection.DeleteAll();

            var produtos = new List<LiteDbProduto>
            {
                new LiteDbProduto
                {
                    Id = ObjectId.NewObjectId(),
                    Nome = "Teclado Mecânico",
                    Preco = 250.00m,
                    Categoria = "Periféricos",
                    Linhas = new List<LiteDbProdutoLine> { new LiteDbProdutoLine { Name = "A" }, new LiteDbProdutoLine { Name = "B" } }
                },
                new LiteDbProduto
                {
                    Id = ObjectId.NewObjectId(),
                    Nome = "Rato Gamer",
                    Preco = 120.00m,
                    Categoria = "Periféricos",
                    Linhas = new List<LiteDbProdutoLine> { new LiteDbProdutoLine { Name = "A" } }
                },
                new LiteDbProduto
                {
                    Id = ObjectId.NewObjectId(),
                    Nome = "Monitor 24\"",
                    Preco = 900.00m,
                    Categoria = "Monitores",
                    Linhas = new List<LiteDbProdutoLine> { new LiteDbProdutoLine { Name = "C" } }
                },
                new LiteDbProduto
                {
                    Id = ObjectId.NewObjectId(),
                    Nome = "Cadeira Gamer",
                    Preco = 1200.00m,
                    Categoria = "Mobiliário",
                    Linhas = new List<LiteDbProdutoLine> { new LiteDbProdutoLine { Name = "B" } }
                }
            };

            collection.InsertBulk(produtos);
        }
    }
    public class LiteDbDataStoreForTest
    {
        private readonly LiteDbTestFixture _fixture;

        public LiteDbDataStoreForTest(LiteDbTestFixture fixture)
        {
            _fixture = fixture;
        }

        public void UpdateMany<T>(IABFFilter<T> filter, ABUpdateDefinition<T> updateDefinition) where T : class
        {
            var collection = _fixture.Database.GetCollection<T>(_fixture.CollectionName);
            var predicate = filter.ToExpression();

            var documentsToUpdate = collection.Query().Where(predicate).ToList();

            if (!documentsToUpdate.Any()) return;

            foreach (var doc in documentsToUpdate)
            {
                foreach (var assignment in updateDefinition.Assignments)
                {
                    var propertyInfo = typeof(T).GetProperty(assignment.Key);
                    if (propertyInfo != null && propertyInfo.CanWrite)
                    {
                        propertyInfo.SetValue(doc, assignment.Value);
                    }
                }
            }
            collection.Update(documentsToUpdate);
        }
    }
    public class LiteDBFilterTest : IClassFixture<LiteDbTestFixture>
    {
        private readonly LiteDbTestFixture _fixture;

        public LiteDBFilterTest(LiteDbTestFixture fixture)
        {
            _fixture = fixture;
        }

        private ILiteCollection<LiteDbProduto> GetCollection()
        {
            return _fixture.Database.GetCollection<LiteDbProduto>(_fixture.CollectionName);
        }

        [Fact]
        public void TestEqFilter()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<LiteDbProduto>();
            var filter = builder.Eq(p => p.Categoria, "Periféricos");

            // O LiteDB suporta AsQueryable() nativamente com expressões
            var resultado = collection.Query()
                .Where(filter.ToExpression())
                .ToList();

            Assert.Equal(2, resultado.Count);
        }

        [Fact]
        public void TestNeFilter()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<LiteDbProduto>();
            var filter = builder.Ne(p => p.Categoria, "Periféricos");

            var resultado = collection.Query()
                .Where(filter.ToExpression())
                .ToList();

            Assert.Equal(2, resultado.Count);
        }

        [Fact]
        public void TestGteAndLteFilters()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<LiteDbProduto>();
            var filter = builder.And(
                builder.Gte(p => p.Preco, 200.00m),
                builder.Lte(p => p.Preco, 1000.00m)
            );

            var resultado = collection.Query()
                .Where(filter.ToExpression())
                .ToList();

            Assert.Equal(2, resultado.Count);
        }

        [Fact]
        public void TestInAndNotInFilters()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<LiteDbProduto>();

            var filterIn = builder.In(p => p.Categoria, new[] { "Monitores", "Mobiliário" });
            var resultadoIn = collection.Query()
                .Where(filterIn.ToExpression())
                .ToList();
            Assert.Equal(2, resultadoIn.Count);

            var filterNotIn = builder.NotIn(p => p.Categoria, new[] { "Periféricos" });
            var resultadoNotIn = collection.Query()
                .Where(filterNotIn.ToExpression())
                .ToList();
            Assert.Equal(2, resultadoNotIn.Count);
        }

        [Fact]
        public void TestRegexFilter()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<LiteDbProduto>();
            var filter = builder.Regex(p => p.Nome, "Gamer");

            var resultado = collection.Query()
                .Where(filter.ToExpression())
                .ToList();

            Assert.Equal(2, resultado.Count);
        }

        [Fact]
        public void TestOrFilter()
        {
            var collection = GetCollection();
            var builder = new ABFilterBuilder<LiteDbProduto>();
            var filter = builder.Or(
                builder.Eq(p => p.Categoria, "Monitores"),
                builder.Eq(p => p.Preco, 1200.00m)
            );

            var resultado = collection.Query()
                .Where(filter.ToExpression())
                .ToList();

            Assert.Equal(2, resultado.Count);
        }

        [Fact]
        public void TestSortByPriceAscending()
        {
            var collection = GetCollection();

            var sortBuilder = new ABFSortBuilder<LiteDbProduto>();
            var sortDef = sortBuilder.OrderBy(p => p.Preco).Build();

            var query = collection.Query();
            foreach (var sort in sortDef.GetFields())
                query = sort.Direction  == ABFSortDirection.Ascending ? query.OrderBy(sort.KeySelector) : query.OrderByDescending(sort.KeySelector);

            var resultado = query.ToList();

            Assert.Equal(4, resultado.Count);
            Assert.Equal("Rato Gamer", resultado[0].Nome);
            Assert.Equal("Teclado Mecânico", resultado[1].Nome);
            Assert.Equal("Monitor 24\"", resultado[2].Nome);
            Assert.Equal("Cadeira Gamer", resultado[3].Nome);
        }

        [Fact]
        public void TestPagination_SkipAndTake()
        {
            var collection = GetCollection();

            var finder = new ABFinder<LiteDbProduto>
            {
                Sort = new ABFSortBuilder<LiteDbProduto>().OrderBy(p => p.Preco).Build(),
                Skip = 1,
                Take = 2
            };

            var query = collection.Query();
            foreach (var sort in finder.Sort.GetFields())
                query = sort.Direction == ABFSortDirection.Ascending ? query.OrderBy(sort.KeySelector) : query.OrderByDescending(sort.KeySelector);

            var resultado = query
                //.Where(finder.Filter.ToExpression())
                .Skip(finder.Skip ?? 0)
                .Limit(finder.Take ?? 0)
                .ToList();

            Assert.Equal(2, resultado.Count);
            Assert.Equal("Teclado Mecânico", resultado[0].Nome);
            Assert.Equal("Monitor 24\"", resultado[1].Nome);
        }

        [Fact]
        public void TestProjection()
        {
            var collection = GetCollection();

            var finder = new ABFinder<LiteDbProduto, LiteDbProdutoDto>
            {
                Filter = new ABFExpressionFilter<LiteDbProduto>(p => p.Categoria == "Periféricos"),
                Projection = p => new LiteDbProdutoDto
                {
                    NomeDoProduto = p.Nome,
                    Valor = p.Preco
                }
            };

            var resultado = collection.Query()
                .Where(finder.Filter.ToExpression())
                .Select(finder.Projection).ToList();

            Assert.Equal(2, resultado.Count);
            Assert.Contains(resultado, r => r.NomeDoProduto == "Teclado Mecânico" && r.Valor == 250.00m);
            Assert.Contains(resultado, r => r.NomeDoProduto == "Rato Gamer" && r.Valor == 120.00m);
        }
        [Fact]
        public void TestUpdateMany_AgnosticUpdate()
        {
            var collection = GetCollection();

            // 1. Instancia o adaptador de DataStore para o LiteDB na suíte de testes
            var dataStore = new LiteDbDataStoreForTest(_fixture);

            // 2. Prepara o filtro usando o seu ABFilterBuilder (atualizar todos da categoria "Periféricos")
            var filterBuilder = new ABFilterBuilder<LiteDbProduto>();
            var filter = filterBuilder.Eq(p => p.Categoria, "Periféricos");

            // 3. Prepara a definição de atualização usando o UpdateBuilder abstrato
            var updateDefinition = new ABUpdateBuilder<LiteDbProduto>()
                .Set(p => p.Preco, 999.99m)
                .Set(p => p.Categoria, "Super Periféricos")
                .Build();

            // 4. Executa a atualização através da camada agnóstica
            dataStore.UpdateMany(filter, updateDefinition);

            // 5. Validação direta na coleção do LiteDB para confirmar as alterações
            var perifericosAtualizados = collection.Query()
                .Where(p => p.Categoria == "Super Periféricos")
                .ToList();

            Assert.Equal(2, perifericosAtualizados.Count);
            Assert.All(perifericosAtualizados, p => Assert.Equal(999.99m, p.Preco));

            // Garante que registros de outras categorias não foram afetados
            var monitor = collection.Query()
                .Where(p => p.Categoria == "Monitores")
                .FirstOrDefault();

            Assert.NotNull(monitor);
            Assert.Equal(900.00m, monitor.Preco);
        }
    }
}
