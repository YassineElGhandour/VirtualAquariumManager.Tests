using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VirtualAquariumManager.Controllers;
using VirtualAquariumManager.Data;
using VirtualAquariumManager.Models;
using VirtualAquariumManager.ViewModels;
using Task = System.Threading.Tasks.Task;

namespace VirtualAquariumManager.Tests.Controllers
{
    public class TankControllerTests
    {
        private static ApplicationDbContext InMemoryDbContext
        {
            get
            {
                var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                    .UseInMemoryDatabase(Guid.NewGuid().ToString())
                    .Options;

                var context = new ApplicationDbContext(options);

                context.Tank.AddRange(
                    new Tank
                    {
                        Id = Guid.NewGuid(),
                        Shape = "Round",
                        Size = 50m,
                        CreatedDate = new DateTime(2025, 1, 1),
                        WaterQuality = new WaterQuality
                        {
                            Id = Guid.NewGuid(),
                            PhLevel = 7.2m,
                            Temperature = 22.5m,
                            AmmoniaLevel = 0.1m,
                            WaterType = "Freshwater",
                            CreatedDate = new DateTime(2025, 1, 1)
                        }
                    },
                    new Tank
                    {
                        Id = Guid.NewGuid(),
                        Shape = "Square",
                        Size = 100m,
                        CreatedDate = new DateTime(2025, 2, 15),
                        WaterQuality = new WaterQuality
                        {
                            Id = Guid.NewGuid(),
                            PhLevel = 8.0m,
                            Temperature = 24.0m,
                            AmmoniaLevel = 0.05m,
                            WaterType = "Saltwater",
                            CreatedDate = new DateTime(2025, 2, 15)
                        }
                    }
                );
                context.SaveChanges();

                return context;
            }
        }

        [Fact]
        public async Task Index_NoSearch_ReturnsAllTanks()
        {
            var controller = new TanksController(InMemoryDbContext);
            var result = await controller.Index(null) as ViewResult;
            var model = Assert.IsType<List<TankWaterQualityData>>(result?.Model, exactMatch: false);
            Assert.Equal(2, model.Count);
        }

        [Theory]
        [InlineData("Round", 1)]
        [InlineData("Saltwater", 1)]
        [InlineData("100", 1)]
        [InlineData("2025-01-01", 1)]
        public async Task Index_SearchFiltersCorrectly(string searchString, int expectedCount)
        {
            var controller = new TanksController(InMemoryDbContext);
            var result = await controller.Index(searchString) as ViewResult;
            var model = Assert.IsType<List<TankWaterQualityData>>(result?.Model, exactMatch: false);
            Assert.Equal(expectedCount, model.Count);
        }

        [Fact]
        public async Task Index_InvalidDecimalSearch_DoesNotThrowAndFiltersByTextOnly()
        {
            var controller = new TanksController(InMemoryDbContext);
            var ex = await Record.ExceptionAsync(() => controller.Index("notanumber"));
            Assert.Null(ex);

            var result = await controller.Index("notanumber") as ViewResult;
            var model = Assert.IsType<List<TankWaterQualityData>>(result?.Model, exactMatch: false);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Index_InvalidPageNumber_ResetsToFirstPage()
        {
            var controller = new TanksController(InMemoryDbContext);

            var result = await controller.Index(null, page: 0, pageSize: 10) as ViewResult;
            var model = Assert.IsType<List<TankWaterQualityData>>(result?.Model, exactMatch: false);
            Assert.Equal(2, model.Count);
        }

        [Fact]
        public async Task Index_PageTooHigh_ReturnsEmptyList()
        {
            var controller = new TanksController(InMemoryDbContext);

            var result = await controller.Index(null, page: 2, pageSize: 2) as ViewResult;
            var model = Assert.IsType<List<TankWaterQualityData>>(result?.Model, exactMatch: false);
            Assert.Empty(model);
        }

        [Fact]
        public async Task Index_NullSearchOrWhitespace_TreatedAsNoFilter()
        {
            var controller = new TanksController(InMemoryDbContext);
            var ResultEmptySearch = await controller.Index("   ") as ViewResult;
            var ModelEmptySearch = Assert.IsType<List<TankWaterQualityData>>(ResultEmptySearch?.Model, exactMatch: false);
            Assert.Equal(2, ModelEmptySearch.Count);

            var ResultNullSearch = await controller.Index(string.Empty) as ViewResult;
            var ModelNullSearch = Assert.IsAssignableFrom<List<TankWaterQualityData>>(ResultNullSearch?.Model);
            Assert.Equal(2, ModelNullSearch.Count);
        }

        [Fact]
        public async Task Index_Pagination_WorksCorrectly()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            for (int i = 1; i <= 15; i++)
            {
                context.Tank.Add(new Tank
                {
                    Id = Guid.NewGuid(),
                    Shape = "Shape" + i,
                    Size = i * 10m,
                    CreatedDate = DateTime.Today.AddDays(-i),
                    WaterQuality = new WaterQuality
                    {
                        Id = Guid.NewGuid(),
                        PhLevel = 7.0m + i / 10m,
                        Temperature = 20 + i,
                        AmmoniaLevel = 0.01m * i,
                        WaterType = i % 2 == 0 ? "Freshwater" : "Saltwater",
                        CreatedDate = DateTime.Today.AddDays(-i)
                    }
                });
            }
            context.SaveChanges();
            var controller = new TanksController(context);
            var result = await controller.Index(null, page: 2, pageSize: 10) as ViewResult;
            var model = Assert.IsType<List<TankWaterQualityData>>(result?.Model, exactMatch: false);
            Assert.Equal(5, model.Count);
        }


        [Fact]
        public async Task Details_NonexistentId_ReturnsNotFound()
        {
            var controller = new TanksController(InMemoryDbContext);
            var result = await controller.Details(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Details_ValidId_ReturnsViewWithCorrectModel()
        {
            var context = InMemoryDbContext;

            var TankId = Guid.NewGuid();
            var created = DateTime.UtcNow;

            context.Tank.Add(new Tank
            {
                Id = TankId,
                Shape = "Hexagon",
                Size = 75.5m,
                CreatedDate = created,
                WaterQuality = new WaterQuality
                {
                    Id = Guid.NewGuid(),
                    PhLevel = 6.8m,
                    Temperature = 23.3m,
                    AmmoniaLevel = 0.02m,
                    WaterType = "Freshwater",
                    CreatedDate = created
                }
            });
            await context.SaveChangesAsync();

            var controller = new TanksController(context);
            var actionResult = await controller.Details(TankId);

            var viewResult = Assert.IsType<ViewResult>(actionResult);
            var model = Assert.IsType<TankWaterQualityData>(viewResult.Model);

            Assert.Equal(TankId, model.TankId);
            Assert.Equal("Hexagon", model.Shape);
            Assert.Equal(75.5m, model.Size);
            Assert.Equal(6.8m, model.PhLevel);
            Assert.Equal(23.3m, model.Temperature);
            Assert.Equal(0.02m, model.AmmoniaLevel);
            Assert.Equal("Freshwater", model.WaterType);
            Assert.Equal(created, model.CreatedDate);
        }


        [Fact]
        public void Create_Get_ReturnsView()
        {
            var controller = new TanksController(InMemoryDbContext);
            var result = controller.Create();
            Assert.IsType<ViewResult>(result);
        }

        [Fact]
        public async Task Create_Post_ValidModel_RedirectsAndAddsTank()
        {
            // Arrange
            var context = InMemoryDbContext;
            var controller = new TanksController(context);
            var now = DateTime.UtcNow;
            var tank = new Tank
            {
                Shape = "Circle",
                Size = 30m,
                CreatedDate = now,
                WaterQuality = new WaterQuality
                {
                    Id = Guid.NewGuid(),
                    PhLevel = 7.0m,
                    Temperature = 21.0m,
                    AmmoniaLevel = 0.0m,
                    WaterType = "Freshwater",
                    CreatedDate = now
                }
            };

            // Act
            var result = await controller.Create(tank) as RedirectToActionResult;

            // Assert
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            var saved = context.Tank.Include(t => t.WaterQuality).Single(t => t.Shape == "Circle");
            Assert.Equal("Circle", saved.Shape);
            Assert.Equal(30m, saved.Size);
            Assert.Equal(now, saved.CreatedDate);
            Assert.NotEqual(Guid.Empty, saved.Id);
            Assert.Equal(7.0m, saved.WaterQuality.PhLevel);
            Assert.Equal(21.0m, saved.WaterQuality.Temperature);
            Assert.Equal(0.0m, saved.WaterQuality.AmmoniaLevel);
            Assert.Equal("Freshwater", saved.WaterQuality.WaterType);
        }

        [Fact]
        public async Task Create_Post_InvalidModel_ReturnsViewWithModel()
        {
            var context = InMemoryDbContext;
            var controller = new TanksController(context);
            controller.ModelState.AddModelError("Shape", "Required");
            var tank = new Tank
            {
                Shape = "",
                Size = 10m,
                CreatedDate = DateTime.UtcNow,
                WaterQuality = new WaterQuality
                {
                    Id = Guid.NewGuid(),
                    PhLevel = 7.5m,
                    Temperature = 22.0m,
                    AmmoniaLevel = 0.1m,
                    WaterType = "Saltwater",
                    CreatedDate = DateTime.UtcNow
                }
            };
            var initialCount = context.Tank.Count();
            var result = await controller.Create(tank) as ViewResult;

            Assert.NotNull(result);
            Assert.Same(tank, result.Model);
            Assert.Equal(initialCount, context.Tank.Count());
        }


        [Fact]
        public async Task Edit_Get_NullId_ReturnsNotFound()
        {
            var context = InMemoryDbContext;
            var controller = new TanksController(context);

            var result = await controller.Edit(null);

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_NonexistentId_ReturnsNotFound()
        {
            var context = InMemoryDbContext;
            var controller = new TanksController(context);

            var result = await controller.Edit(Guid.NewGuid());

            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Get_ValidId_ReturnsViewWithModel()
        {
            var context = InMemoryDbContext;
            var TankId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            context.Tank.Add(new Tank
            {
                Id = TankId,
                Shape = "Triangle",
                Size = 40m,
                CreatedDate = now,
                WaterQuality = new WaterQuality
                {
                    Id = Guid.NewGuid(),
                    PhLevel = 7.1m,
                    Temperature = 20.5m,
                    AmmoniaLevel = 0.03m,
                    WaterType = "Freshwater",
                    CreatedDate = now
                }
            });
            await context.SaveChangesAsync();

            var controller = new TanksController(context);
            var result = await controller.Edit(TankId) as ViewResult;
            var model = Assert.IsType<TankWaterQualityData>(result.Model);

            Assert.Equal(TankId, model.TankId);
            Assert.Equal("Triangle", model.Shape);
            Assert.Equal(40m, model.Size);
            Assert.Equal(7.1m, model.PhLevel);
            Assert.Equal(20.5m, model.Temperature);
            Assert.Equal(0.03m, model.AmmoniaLevel);
            Assert.Equal("Freshwater", model.WaterType);
            Assert.Equal(now, model.CreatedDate);
        }

        [Fact]
        public async Task Edit_Post_InvalidModel_ReturnsViewWithModel()
        {
            var context = InMemoryDbContext;
            var TankId = Guid.NewGuid();
            var now = DateTime.UtcNow;

            context.Tank.Add(new Tank
            {
                Id = TankId,
                Shape = "Orig",
                Size = 20m,
                CreatedDate = now,
                WaterQuality = new WaterQuality
                {
                    Id = Guid.NewGuid(),
                    PhLevel = 6.9m,
                    Temperature = 19m,
                    AmmoniaLevel = 0.02m,
                    WaterType = "Saltwater",
                    CreatedDate = now
                }
            });
            await context.SaveChangesAsync();

            var controller = new TanksController(context);
            controller.ModelState.AddModelError("Shape", "Required");

            var vm = new TankWaterQualityData
            {
                TankId = TankId,
                Shape = "",
                Size = 25m,
                WaterQuality = new WaterQuality
                {
                    PhLevel = 7.2m,
                    Temperature = 22m,
                    AmmoniaLevel = 0.01m,
                    WaterType = "Freshwater",
                    CreatedDate = now
                },
                PhLevel = 7.2m,
                Temperature = 22m,
                AmmoniaLevel = 0.01m,
                WaterType = "Freshwater",
                CreatedDate = now
            };

            var result = await controller.Edit(TankId, vm) as ViewResult;
            Assert.NotNull(result);
            Assert.Same(vm, result.Model);

            var unchanged = context.Tank
                .Include(t => t.WaterQuality)
                .Single(t => t.Id == TankId);

            Assert.Equal("Orig", unchanged.Shape);
            Assert.Equal(20m, unchanged.Size);
            Assert.Equal(6.9m, unchanged.WaterQuality.PhLevel);
        }

        [Fact]
        public async Task Edit_Post_NonexistentId_ReturnsNotFound()
        {
            var context = InMemoryDbContext;
            var controller = new TanksController(context);
            var TankId = Guid.NewGuid();

            var vm = new TankWaterQualityData
            {
                TankId = TankId,
                Shape = "Any",
                Size = 10m,
                WaterQuality = new WaterQuality
                {
                    PhLevel = 7m,
                    Temperature = 20m,
                    AmmoniaLevel = 0.1m,
                    WaterType = "Freshwater",
                    CreatedDate = DateTime.UtcNow
                },
                PhLevel = 7m,
                Temperature = 20m,
                AmmoniaLevel = 0.1m,
                WaterType = "Freshwater",
                CreatedDate = DateTime.UtcNow
            };

            var result = await controller.Edit(TankId, vm);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Edit_Post_ValidModel_UpdatesAndRedirects()
        {
            var context = InMemoryDbContext;
            var TankId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            context.Tank.Add(new Tank
            {
                Id = TankId,
                Shape = "Old",
                Size = 50m,
                CreatedDate = now,
                WaterQuality = new WaterQuality
                {
                    Id = Guid.NewGuid(),
                    PhLevel = 6.5m,
                    Temperature = 18m,
                    AmmoniaLevel = 0.05m,
                    WaterType = "Saltwater",
                    CreatedDate = now
                }
            });
            await context.SaveChangesAsync();

            var controller = new TanksController(context);

            var vm = new TankWaterQualityData
            {
                TankId = TankId,
                Shape = "New",
                Size = 60m,
                WaterQuality = new WaterQuality
                {
                    PhLevel = 7.8m,
                    Temperature = 24m,
                    AmmoniaLevel = 0.02m,
                    WaterType = "Freshwater",
                    CreatedDate = now
                },
                PhLevel = 7.8m,
                Temperature = 24m,
                AmmoniaLevel = 0.02m,
                WaterType = "Freshwater",
                CreatedDate = now
            };

            var result = await controller.Edit(TankId, vm) as RedirectToActionResult;
            Assert.NotNull(result);
            Assert.Equal("Details", result.ActionName);
            Assert.Equal(TankId, result.RouteValues["id"]);

            var updated = context.Tank
                .Include(t => t.WaterQuality)
                .Single(t => t.Id == TankId);

            Assert.Equal("New", updated.Shape);
            Assert.Equal(60m, updated.Size);
            Assert.Equal(7.8m, updated.WaterQuality.PhLevel);
            Assert.Equal(24m, updated.WaterQuality.Temperature);
            Assert.Equal(0.02m, updated.WaterQuality.AmmoniaLevel);
            Assert.Equal("Freshwater", updated.WaterQuality.WaterType);
        }

        [Fact]
        public async Task Delete_Get_NullId_ReturnsNotFound()
        {
            var context = InMemoryDbContext;
            var controller = new TanksController(context);
            var result = await controller.Delete(null);
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_NonexistentId_ReturnsNotFound()
        {
            var context = InMemoryDbContext;
            var controller = new TanksController(context);
            var result = await controller.Delete(Guid.NewGuid());
            Assert.IsType<NotFoundResult>(result);
        }

        [Fact]
        public async Task Delete_Get_ValidId_ReturnsViewWithModel()
        {
            var context = InMemoryDbContext;
            var TankId = Guid.NewGuid();
            var now = DateTime.UtcNow;
            context.Tank.Add(new Tank
            {
                Id = TankId,
                Shape = "Rectangular",
                Size = 45m,
                CreatedDate = now,
                WaterQuality = new WaterQuality
                {
                    PhLevel = 7.0m,
                    Temperature = 22.0m,
                    AmmoniaLevel = 0.1m,
                    WaterType = "Freshwater",
                    CreatedDate = now
                }
            });
            await context.SaveChangesAsync();
            var controller = new TanksController(context);
            var result = await controller.Delete(TankId) as ViewResult;
            var model = Assert.IsType<Tank>(result.Model);
            Assert.Equal(TankId, model.Id);
            Assert.Equal("Rectangular", model.Shape);
            Assert.Equal(45m, model.Size);
            Assert.Equal(now, model.CreatedDate);
        }

        [Fact]
        public async Task DeleteConfirmed_ValidId_RemovesTankAndRedirects()
        {
            var context = InMemoryDbContext;
            var TankId = Guid.NewGuid();
            context.Tank.Add(new Tank
            {
                Id = TankId,
                Shape = "ToDelete",
                Size = 20m,
                CreatedDate = DateTime.UtcNow,
                WaterQuality = new WaterQuality
                {
                    PhLevel = 6.5m,
                    Temperature = 19.0m,
                    AmmoniaLevel = 0.05m,
                    WaterType = "Saltwater",
                    CreatedDate = DateTime.UtcNow
                }
            });
            await context.SaveChangesAsync();
            var controller = new TanksController(context);
            var result = await controller.DeleteConfirmed(TankId) as RedirectToActionResult;
            Assert.NotNull(result);
            Assert.Equal("Index", result.ActionName);
            Assert.Null(await context.Tank.FindAsync(TankId));
        }
    }
}