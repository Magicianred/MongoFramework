using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using MongoFramework.Attributes;

namespace MongoFramework.Tests
{
	[TestClass]
	public class MongoDbTenantSetTests : TestBase
	{
		public class TestModel : IHaveTenantId
		{
			public string Id { get; set; }
			public string TenantId { get; set; }

			public string Description { get; set; }
			public bool BooleanField { get; set; }
		}
		public class TestUniqueModel : IHaveTenantId
		{
			public string Id { get; set; }
			public string TenantId { get; set; }

			[Index(IndexType.Standard, IsUnique = true, IsTenantExclusve = true)]
			public string UserName { get; set; }
		}
		[TestMethod]
		public void SuccessfulCreateTenantId()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var model = dbSet.Create();

			Assert.AreEqual(tenantId, model.TenantId);
		}

		[TestMethod]
		public void SuccessfulLimitsQueryToTenant()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var context2 = new MongoDbTenantContext(connection, tenantId + "-alt");
			var dbSet2 = new MongoDbTenantSet<TestModel>(context2);


			var entities = new[] {
				new TestModel
				{
					Description = "SuccessfulLimitsQueryToTenant.1"
				},
				new TestModel
				{
					Description = "SuccessfulLimitsQueryToTenant.2",
					BooleanField = true
				}
			};

			var entities2 = new[] {
				new TestModel
				{
					Description = "SuccessfulLimitsQueryToTenant.1"
				},
				new TestModel
				{
					Description = "SuccessfulLimitsQueryToTenant.2",
					BooleanField = true
				}
			};

			dbSet.AddRange(entities);
			dbSet2.AddRange(entities2);

			context.SaveChanges();
			context2.SaveChanges();

			Assert.AreEqual(2, dbSet.Count());
			Assert.AreEqual(2, dbSet2.Count());

			Assert.AreEqual(1, dbSet.Where(e => e.BooleanField).Count());
			Assert.AreEqual(1, dbSet2.Where(e => e.BooleanField).Count());

		}

		[TestMethod]
		public void SuccessfulInsertAndQueryBack()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			dbSet.Add(new TestModel
			{
				Description = "SuccessfulInsertAndQueryBack"
			});

			Assert.IsFalse(dbSet.Any(m => m.Description == "SuccessfulInsertAndQueryBack"));
			context.SaveChanges();
			Assert.IsTrue(dbSet.Any(m => m.Description == "SuccessfulInsertAndQueryBack"));
			Assert.IsTrue(dbSet.Any(m => m.TenantId == tenantId));
		}

		[TestMethod]
		public async Task SuccessfulInsertAndQueryBackAsync()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			dbSet.Add(new TestModel
			{
				Description = "SuccessfulInsertAndQueryBackAsync"
			});

			Assert.IsFalse(dbSet.Any(m => m.Description == "SuccessfulInsertAndQueryBackAsync"));
			await context.SaveChangesAsync();
			Assert.IsTrue(dbSet.Any(m => m.Description == "SuccessfulInsertAndQueryBackAsync"));
			Assert.IsTrue(dbSet.Any(m => m.TenantId == tenantId));
		}

		[TestMethod]
		public void SuccessfullyUpdateEntity()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var entity = new TestModel
			{
				Description = "SuccessfullyUpdateEntity"
			};

			dbSet.Add(entity);
			context.SaveChanges();

			dbSet = new MongoDbTenantSet<TestModel>(context);

			entity.Description = "SuccessfullyUpdateEntity-Updated";
			dbSet.Update(entity);

			Assert.IsFalse(dbSet.Any(m => m.Description == "SuccessfullyUpdateEntity-Updated"));
			context.SaveChanges();
			Assert.IsTrue(dbSet.Any(m => m.Description == "SuccessfullyUpdateEntity-Updated"));
			Assert.IsTrue(dbSet.First(m => m.Description == "SuccessfullyUpdateEntity-Updated").TenantId == tenantId);
		}


		[TestMethod]
		public void SuccessfullyBlocksUpdateEntity()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var entity = new TestModel
			{
				Description = "SuccessfullyBlocksUpdateEntity"
			};

			dbSet.Add(entity);
			context.SaveChanges();

			dbSet = new MongoDbTenantSet<TestModel>(context);
			entity.TenantId = "qweasd";
			entity.Description = "SuccessfullyBlocksUpdateEntity-Updated";
			Assert.ThrowsException<MultiTenantException>(() => dbSet.Update(entity));

		}

		[TestMethod]
		public void SuccessfullyBlocksUpdateChangedEntity()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var entity = new TestModel
			{
				Description = "SuccessfullyBlocksUpdateChangedEntity"
			};

			dbSet.Add(entity);
			context.SaveChanges();
			entity.Description = "SuccessfullyBlocksUpdateChangedEntity-Updated";

			dbSet.Update(entity);

			//changing tenant ID after state is updated
			entity.TenantId = "qweasd";
			Assert.ThrowsException<MultiTenantException>(() => context.SaveChanges());
		}


		[TestMethod]
		public void SuccessfullyUpdateRange()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var entities = new[] {
				new TestModel
				{
					Description = "SuccessfullyUpdateRange.1"
				},
				new TestModel
				{
					Description = "SuccessfullyUpdateRange.2"
				}
			};

			dbSet.AddRange(entities);
			context.SaveChanges();

			dbSet = new MongoDbTenantSet<TestModel>(context);

			entities[0].Description = "SuccessfullyUpdateRange.1-Updated";
			entities[1].Description = "SuccessfullyUpdateRange.2-Updated";
			dbSet.UpdateRange(entities);

			Assert.IsFalse(dbSet.Any(m => m.Description == "SuccessfullyUpdateRange.1-Updated" || m.Description == "SuccessfullyUpdateRange.2-Updated"));
			context.SaveChanges();
			Assert.IsTrue(dbSet.Any(m => m.Description == "SuccessfullyUpdateRange.1-Updated"));
			Assert.IsTrue(dbSet.Any(m => m.Description == "SuccessfullyUpdateRange.2-Updated"));
		}

		[TestMethod]
		public void SuccessfullyBlocksUpdateRange()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var entities = new[] {
				new TestModel
				{
					Description = "SuccessfullyBlocksUpdateRange.1"
				},
				new TestModel
				{
					Description = "SuccessfullyBlocksUpdateRange.2"
				}
			};

			dbSet.AddRange(entities);
			context.SaveChanges();

			dbSet = new MongoDbTenantSet<TestModel>(context);

			entities[0].Description = "SuccessfullyBlocksUpdateRange.1-Updated";
			entities[0].TenantId = "qweasd";

			entities[1].Description = "SuccessfullyBlocksUpdateRange.2-Updated";
			entities[1].TenantId = "qweasd";
			Assert.ThrowsException<MultiTenantException>(() => dbSet.UpdateRange(entities));
		}

		[TestMethod]
		public void SuccessfullyRemoveEntity()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var entity = new TestModel
			{
				Description = "SuccessfullyRemoveEntity"
			};

			dbSet.Add(entity);
			context.SaveChanges();

			dbSet = new MongoDbTenantSet<TestModel>(context);

			dbSet.Remove(entity);

			Assert.IsTrue(dbSet.Any(m => m.Description == "SuccessfullyRemoveEntity"));
			context.SaveChanges();
			Assert.IsFalse(dbSet.Any(m => m.Description == "SuccessfullyRemoveEntity"));
		}

		[TestMethod]
		public void SuccessfullyBlocksRemoveEntity()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var entity = new TestModel
			{
				Description = "SuccessfullyBlocksRemoveEntity"
			};

			dbSet.Add(entity);
			context.SaveChanges();

			entity.TenantId = "qweasd";

			dbSet = new MongoDbTenantSet<TestModel>(context);

			Assert.ThrowsException<MultiTenantException>(() => dbSet.Remove(entity));

		}

		[TestMethod]
		public void SuccessfullyRemoveRange()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var entities = new[] {
				new TestModel
				{
					Description = "SuccessfullyRemoveRange.1"
				},
				new TestModel
				{
					Description = "SuccessfullyRemoveRange.2"
				}
			};

			dbSet.AddRange(entities);
			context.SaveChanges();

			dbSet = new MongoDbTenantSet<TestModel>(context);

			dbSet.RemoveRange(entities);

			Assert.IsTrue(dbSet.Any(m => m.Description == "SuccessfullyRemoveRange.1"));
			Assert.IsTrue(dbSet.Any(m => m.Description == "SuccessfullyRemoveRange.2"));
			context.SaveChanges();
			Assert.IsFalse(dbSet.Any(m => m.Description == "SuccessfullyRemoveRange.1"));
			Assert.IsFalse(dbSet.Any(m => m.Description == "SuccessfullyRemoveRange.2"));
		}
		
		[TestMethod]
		public void SuccessfullyBlocksRemoveRange()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var entities = new[] {
				new TestModel
				{
					Description = "SuccessfullyBlocksRemoveRange.1"
				},
				new TestModel
				{
					Description = "SuccessfullyBlocksRemoveRange.2"
				}
			};

			dbSet.AddRange(entities);
			context.SaveChanges();

			dbSet = new MongoDbTenantSet<TestModel>(context);

			entities[0].TenantId = "qweasd";
			entities[1].TenantId = "qweasd";

			Assert.ThrowsException<MultiTenantException>(() => dbSet.RemoveRange(entities));

		}

		[TestMethod]
		public void SuccessfullyRemoveEntityById()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var context2 = new MongoDbTenantContext(connection, tenantId + "-alt");
			var dbSet2 = new MongoDbTenantSet<TestModel>(context2);

			var entity = new TestModel
			{
				Description = "SuccessfullyRemoveEntityById"
			};

			dbSet.Add(entity);
			context.SaveChanges();

			var entity2 = new TestModel
			{
				Description = "SuccessfullyRemoveEntityById"
			};

			dbSet2.Add(entity2);
			context2.SaveChanges();

			dbSet = new MongoDbTenantSet<TestModel>(context);

			dbSet.RemoveById(entity.Id);

			//mismatched tenant, should not delete anything
			dbSet.RemoveById(entity2.Id);

			Assert.IsTrue(dbSet.Any(m => m.Description == "SuccessfullyRemoveEntityById"));
			Assert.IsTrue(dbSet2.Any(m => m.Description == "SuccessfullyRemoveEntityById"));
			context.SaveChanges();
			Assert.IsFalse(dbSet.Any(m => m.Description == "SuccessfullyRemoveEntityById"));
			Assert.IsTrue(dbSet2.Any(m => m.Description == "SuccessfullyRemoveEntityById"));
		}

		[TestMethod]
		public async Task SuccessfullyRemoveEntityByIdAsync()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var context2 = new MongoDbTenantContext(connection, tenantId + "-alt");
			var dbSet2 = new MongoDbTenantSet<TestModel>(context2);

			var entity = new TestModel
			{
				Description = "SuccessfullyRemoveEntityByIdAsync"
			};

			dbSet.Add(entity);
			await context.SaveChangesAsync();

			var entity2 = new TestModel
			{
				Description = "SuccessfullyRemoveEntityByIdAsync"
			};

			dbSet2.Add(entity2);
			await context2.SaveChangesAsync();

			dbSet = new MongoDbTenantSet<TestModel>(context);

			dbSet.RemoveById(entity.Id);

			//mismatched tenant, should not delete anything
			dbSet.RemoveById(entity2.Id);

			Assert.IsTrue(dbSet.Any(m => m.Description == "SuccessfullyRemoveEntityByIdAsync"));
			Assert.IsTrue(dbSet2.Any(m => m.Description == "SuccessfullyRemoveEntityByIdAsync"));
			await context.SaveChangesAsync();
			Assert.IsFalse(dbSet.Any(m => m.Description == "SuccessfullyRemoveEntityByIdAsync"));
			Assert.IsTrue(dbSet2.Any(m => m.Description == "SuccessfullyRemoveEntityByIdAsync"));
		}

		[TestMethod]
		public void SuccessfullyRemoveRangeByPredicate()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			var context2 = new MongoDbTenantContext(connection, tenantId + "-alt");
			var dbSet2 = new MongoDbTenantSet<TestModel>(context2);


			var entities = new[] {
				new TestModel
				{
					Description = "SuccessfullyRemoveRangeByPredicate"
				},
				new TestModel
				{
					Description = "SuccessfullyRemoveRangeByPredicate",
					BooleanField = true
				}
			};

			var entities2 = new[] {
				new TestModel
				{
					Description = "SuccessfullyRemoveRangeByPredicate"
				},
				new TestModel
				{
					Description = "SuccessfullyRemoveRangeByPredicate",
					BooleanField = true
				}
			};

			dbSet.AddRange(entities);
			dbSet2.AddRange(entities2);

			context.SaveChanges();
			context2.SaveChanges();

			dbSet = new MongoDbTenantSet<TestModel>(context);

			dbSet.RemoveRange(e => e.BooleanField);

			Assert.AreEqual(2, dbSet.Count(m => m.Description == "SuccessfullyRemoveRangeByPredicate"));
			Assert.AreEqual(2, dbSet2.Count(m => m.Description == "SuccessfullyRemoveRangeByPredicate"));
			context.SaveChanges();
			Assert.AreEqual(1, dbSet.Count(m => m.Description == "SuccessfullyRemoveRangeByPredicate"));
			Assert.AreEqual(2, dbSet2.Count(m => m.Description == "SuccessfullyRemoveRangeByPredicate"));
			Assert.IsNotNull(dbSet.FirstOrDefault(m => m.Id == entities[0].Id));
		}

		[TestMethod]
		public void SuccessfullyReturnsBaseContext()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			Assert.IsInstanceOfType((dbSet as IMongoDbSet<TestModel>).Context, typeof(IMongoDbContext));
		}

		[TestMethod]
		public void SuccessfullyBlocksNulls()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestModel>(context);

			Assert.ThrowsException<ArgumentNullException>(() => dbSet.Add(null));
			Assert.ThrowsException<ArgumentNullException>(() => dbSet.AddRange(null));
			Assert.ThrowsException<ArgumentNullException>(() => dbSet.Update(null));
			Assert.ThrowsException<ArgumentNullException>(() => dbSet.UpdateRange(null));
		}

		[TestMethod]
		public void AllowsUniquesByTenant()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestUniqueModel>(context);

			var context2 = new MongoDbTenantContext(connection, tenantId + "-alt");
			var dbSet2 = new MongoDbTenantSet<TestUniqueModel>(context2);

			dbSet.Add(new TestUniqueModel{UserName = "AllowsUniquesByTenant.1"});
			dbSet.Add(new TestUniqueModel{UserName = "AllowsUniquesByTenant.2"});
			dbSet2.Add(new TestUniqueModel{UserName = "AllowsUniquesByTenant.1"});
			dbSet2.Add(new TestUniqueModel{UserName = "AllowsUniquesByTenant.2"});

			context.SaveChanges();
			context2.SaveChanges();

			Assert.AreEqual(2, dbSet.Count());
			Assert.AreEqual(2, dbSet2.Count());
		}

		[TestMethod]
		public void BlocksDuplicatesByTenant()
		{
			var connection = TestConfiguration.GetConnection();
			var tenantId = TestConfiguration.GetTenantId();
			var context = new MongoDbTenantContext(connection, tenantId);
			var dbSet = new MongoDbTenantSet<TestUniqueModel>(context);

			var context2 = new MongoDbTenantContext(connection, tenantId + "-alt");
			var dbSet2 = new MongoDbTenantSet<TestUniqueModel>(context2);

			dbSet.Add(new TestUniqueModel{UserName = "BlocksDuplicatesByTenant"});
			dbSet2.Add(new TestUniqueModel{UserName = "BlocksDuplicatesByTenant"});

			context.SaveChanges();
			context2.SaveChanges();

			dbSet.Add(new TestUniqueModel{UserName = "BlocksDuplicatesByTenant"});
			Assert.ThrowsException<MongoBulkWriteException<TestUniqueModel>>(() => context.SaveChanges());
		}

	}
}