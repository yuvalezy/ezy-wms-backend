using System;

namespace Service.API.Counting;

internal class CountingCreation : IDisposable {
    public int Entry  { get; private set; }
    public int Number { get; private set; }

    public CountingCreation(int id, int employeeID) {
    }


    public void Dispose() {
        // TODO release managed resources here
    }

    public void Execute() {
        throw new NotImplementedException();
    }

    public void SetClosedLines() {
        throw new NotImplementedException();
    }
}