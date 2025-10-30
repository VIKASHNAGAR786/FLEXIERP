﻿using FLEXIERP.DTOs;
using FLEXIERP.MODELS;

namespace FLEXIERP.BusinesLayer_Interfaces
{
    public interface ICommonMasterService
    {
        public Task<DashboardMetricsDto?> GetDashboardMetricsAsync(string startDate, string endDate);
        public Task<byte[]> GenerateDashboardPdf(string startDate, string endDate);
        public Task<byte[]> GenerateDashboardExcel(string startDate, string endDate);
        public Task<List<ReceivedChequeDto>> GetReceivedChequesAsync(PaginationFilter pagination);

        #region notes
        public Task<int> SaveNoteAsync(SaveNotes note);
        public Task<List<NoteDto>> GetAllNotesAsync();
        public Task<NoteDetailsDto> GetNoteDetailsByIdAsync(int rowid);
        #endregion

    }
}
