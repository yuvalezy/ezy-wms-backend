using Adapters.Common.SBO.Services;
using Adapters.CrossPlatform.SBO.Helpers;
using Core.Enums;
using Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace UnitTests.Integration.ExternalSystems.General;

[TestFixture]
[Category("Integration")]
[Category("ExternalSystem")]
[Category("RequiresSapB1")]
public class AlertTest : BaseExternalTest {
    private ILoggerFactory loggerFactory;
    private IExternalSystemAdapter externalSystemAdapter;
    private SboDatabaseService databaseService;

    [OneTimeSetUp]
    new public void OneTimeSetUp() {
        base.OneTimeSetUp();
        using var scope = factory.Services.CreateScope();
        loggerFactory = scope.ServiceProvider.GetRequiredService<ILoggerFactory>();
        externalSystemAdapter = scope.ServiceProvider.GetRequiredService<IExternalSystemAdapter>();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        databaseService = new SboDatabaseService(configuration);
    }

    [Test]
    [Order(1)]
    public async Task Test_TransferAlert() {
        await sboCompany.ConnectCompany();
        const string query = "select top 1 \"DocEntry\", \"DocNum\" from OWTR order by \"DocEntry\" desc";
        (int docEntry, int docNum) = await databaseService.QuerySingleAsync(query, null, dr => (dr.GetInt32(0), dr.GetInt32(1)));
        var alert = new Alert(sboCompany, loggerFactory){ThrowExceptionOnFailure = true};
        await alert.SendDocumentCreationAlert(AlertableObjectType.Transfer, 111, docNum, docEntry, ["manager"]);
    }

    [Test]
    [Order(2)]
    public async Task Test_GoodsReceiptAlert() {
        await sboCompany.ConnectCompany();
        const string query = "select top 1 \"DocEntry\", \"DocNum\" from OPDN order by \"DocEntry\" desc";
        (int docEntry, int docNum) = await databaseService.QuerySingleAsync(query, null, dr => (dr.GetInt32(0), dr.GetInt32(1)));
        var alert = new Alert(sboCompany, loggerFactory){ThrowExceptionOnFailure = true};
        await alert.SendDocumentCreationAlert(AlertableObjectType.GoodsReceipt, 111, docNum, docEntry, ["manager"]);
    }

    [Test]
    [Order(3)]
    public async Task Test_Counting() {
        await sboCompany.ConnectCompany();
        const string query = "select top 1 \"DocEntry\", \"DocNum\" from OINC order by \"DocEntry\" desc";
        (int docEntry, int docNum) = await databaseService.QuerySingleAsync(query, null, dr => (dr.GetInt32(0), dr.GetInt32(1)));
        var alert = new Alert(sboCompany, loggerFactory){ThrowExceptionOnFailure = true};
        await alert.SendDocumentCreationAlert(AlertableObjectType.InventoryCounting, 111, docNum, docEntry, ["manager"]);
    }
    [Test]
    [Order(4)]
    public async Task Test_PickList() {
        await sboCompany.ConnectCompany();
        const string query = "select top 1 \"AbsEntry\" from OPKL order by \"AbsEntry\" desc";
        int absEntry = await databaseService.QuerySingleAsync(query, null, dr => dr.GetInt32(0));
        var alert = new Alert(sboCompany, loggerFactory){ThrowExceptionOnFailure = true};
        await alert.SendDocumentCreationAlert(AlertableObjectType.PickList, 111, absEntry, absEntry, ["manager"]);
        await alert.SendDocumentCreationAlert(AlertableObjectType.PickListCancellation, 111, absEntry, absEntry, ["manager"]);
    }
    [Test]
    [Order(5)]
    public async Task Test_Adjustment() {
        await sboCompany.ConnectCompany();
        
        const string entryQuery = "select top 1 \"DocEntry\", \"DocNum\" from OIGN order by \"DocEntry\" desc";
        (int entryEntry, int entryNumber) = await databaseService.QuerySingleAsync(entryQuery, null, dr => (dr.GetInt32(0), dr.GetInt32(1)));
        
        const string exitQuery = "select top 1 \"DocEntry\", \"DocNum\" from OIGE order by \"DocEntry\" desc";
        (int exitEntry, int exitNumber) = await databaseService.QuerySingleAsync(exitQuery, null, dr => (dr.GetInt32(0), dr.GetInt32(1)));
        
        var alert = new Alert(sboCompany, loggerFactory){ThrowExceptionOnFailure = true};
        await alert.SendDocumentCreationAlert(AlertableObjectType.ConfirmationAdjustments, 111, entryNumber, entryEntry, ["manager"]);
    }

    [OneTimeTearDown]
    new public async Task OneTimeTearDown() {
        await base.OneTimeTearDown();
        loggerFactory?.Dispose();
    }
}