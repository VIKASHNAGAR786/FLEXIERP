using FLEXIERP.DTOs;
using FLEXIERP.MODELS;

namespace FLEXIERP.DataAccessLayer_Interfaces
{
    public interface ICommonMasterRepo
    {

        #region Number System
        public Task<string> GetInvoiceNumber();
        public Task<int> UpdateInvoiceNumber(int updatedBy);

        #endregion
        public Task<int> SaveChequePaymentAsync(SaveChequePaymentDto chequePayment);
        public Task<int> SaveCashPaymentAsync(SaveCashPaymentDto cashPayment);
        public Task<DashboardMetricsDto?> GetDashboardMetricsAsync(string startDate, string endDate);
        public Task<int> SaveUserErrorLogAsync(UserErrorLogDto errorLog);

        #region cheque details 
        public Task<List<ReceivedChequeDto>> GetReceivedChequesAsync(PaginationFilter pagination);
        #endregion

        #region notes
        public Task<int> SaveNoteAsync(SaveNotes note);
        public Task<List<NoteDto>> GetAllNotesAsync();
        public Task<NoteDetailsDto> GetNoteDetailsByIdAsync(int rowid);
        public Task<int> DeleteNotesById(int deletednotsid);
        public Task<int> MarkPinned(int notesid);
        #endregion
    }
}
