using Adapters.CrossPlatform.SBO.Services;
using Core.Interfaces;
using Core.Models.Settings;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UnitTests.Integration.ExternalSystems.Shared;
using WebApi;

namespace UnitTests.Integration.ExternalSystems.General;

[TestFixture]
[Category("Integration")]
[Category("ExternalSystem")]
[Category("RequiresSapB1")]

public class ItemsCustomFieldsTest : BaseExternalTest {
    private ItemData                       itemData;

    private readonly string testWarehouse = TestConstants.SessionInfo.Warehouse;


    [Test]
    [Order(1)]
    public async Task Test_01_CreateNewItem() {
        Assert.That(settings.CustomFields.ContainsKey("Items"), "Custom Fields for Items was not found");
        var helper = new CreateTestItem(sboCompany);
        itemData = await helper.Execute();
    }

    [Test]
    [Order(2)]
    public async Task Test_02_CreateGoodsReceipt_ShouldAddItemToSystemBin() {
        if (!settings.Filters.InitialCountingBinEntry.HasValue) {
            throw new Exception("InitialCountingBinEntry is not set in appsettings.json filters");
        }

        var helper = new CreateGoodsReceipt(sboCompany, itemData.ItemCode, settings, goodsReceiptSeries, factory);
        await helper.Execute();
    }

    [Test]
    [Order(3)]
    public async Task Test_02_ItemStock_ValidateData() {
        Assert.That(itemData.ItemCode, Is.Not.Null);
        var scope    = factory.Services.CreateScope();
        var service  = scope.ServiceProvider.GetRequiredService<IPublicService>();
        var response = (await service.ItemCheckAsync(itemData.ItemCode, null)).ToArray();
        Validate();
        response = (await service.ItemCheckAsync(null, itemData.ItemCode)).ToArray();
        Validate();

        void Validate() {
            Assert.That(response, Is.Not.Null);
            Assert.That(response.Length, Is.EqualTo(1));
            var validate = response[0];
            Assert.That(validate.ItemCode, Is.EqualTo(itemData.ItemCode));
            Assert.That(validate.ItemName, Is.EqualTo(itemData.ItemName));
            Assert.That(validate.NumInBuy, Is.EqualTo(itemData.PurchaseItemsPerUnit));
            Assert.That(validate.BuyUnitMsr, Is.EqualTo(itemData.PurchaseUnit));
            Assert.That(validate.PurPackUn, Is.EqualTo(itemData.PurchaseQtyPerPackUnit));
            Assert.That(validate.PurPackMsr, Is.EqualTo(itemData.PurchasePackagingUnit));
            Assert.That(validate.Barcodes, Contains.Item(itemData.BarCode));

            settings.CustomFields.TryGetValue("Items", out var customFields);
            foreach (var customField in customFields!) {
                Assert.That(validate.CustomFields.TryGetValue(customField.Key, out object? customValue), Is.True);
                Assert.That(customValue != null);
                switch (customField.Type) {
                    case CustomFieldType.Text:
                        Assert.That(customValue, Is.TypeOf<string>());
                        Assert.That(!string.IsNullOrWhiteSpace(customValue as string));
                        break;
                    case CustomFieldType.Number:
                        Assert.That(customValue, Is.TypeOf<decimal>() | Is.TypeOf<double>() | Is.TypeOf<int>() | Is.TypeOf<long>());
                        break;
                    case CustomFieldType.Date:
                        Assert.That(customValue, Is.TypeOf<DateTime>());
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}