using Enset.Application.Imports.Models;

namespace Enset.Application.Exports.Abstractions;

public interface IExcelWriter
{
    void UpdateWorkbook(
        string sourceFilePath,
        string targetFilePath,
        IReadOnlyList<CustomerExcelRow> customers,
        IReadOnlyList<BuildingExcelRow> buildings);

//-------------------------------Customer Updates------------------------------
    bool UpdateCustomerField(
    string sourceFilePath,
    string targetFilePath,
    string customerId,
    string columnName,
    string value);
    
    bool UpdateCustomerId(
    string sourceFilePath,
    string targetFilePath,
    int rowNumber,
    string newCustomerId);

    bool UpdateCustomerNotes(
    string sourceFilePath,
    string targetFilePath,
    string internalCustomerId,
    string notes);

//--------------------------------Building Updates------------------------------
    bool UpdateBuildingField(
    string sourceFilePath,
    string targetFilePath,
    string buildingId,
    string columnName,
    string value);

    bool UpdateBuildingId(
    string sourceFilePath,
    string targetFilePath,
    int rowNumber,
    string newBuildingId);

    bool UpdateBuildingNotes(
    string sourceFilePath,
    string targetFilePath,
    string internalBuildingId,
    string notes);
}

