using Core.DTOs.Transfer;

namespace Core.Interfaces;

public interface ITransferReportService {
    /// <summary>
    /// Builds the supervisor's whole-document view of a transfer: one row per item, carrying the source bins
    /// it was taken from and the target bins it was placed into.
    /// </summary>
    /// <param name="id">The transfer to report on.</param>
    /// <exception cref="KeyNotFoundException">No transfer with that id exists.</exception>
    Task<TransferSummaryReportResponse> GetTransferSummaryReport(Guid id);
}
