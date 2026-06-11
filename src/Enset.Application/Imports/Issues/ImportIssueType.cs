namespace Enset.Application.Imports.Issues;

public enum ImportIssueType
{
    DuplicateCustomer = 1,
    DuplicateBuilding = 2,
    DuplicateMeter = 3,
    InvalidValue = 100,
    InvalidTimestamp = 101
}

/*DuplicateCustomer
DuplicateBuilding
DuplicateMeter

MissingCustomer
MissingBuilding
MissingMeter

InvalidAddress
InvalidPostalCode

InvalidMeterNumber

InvalidTimestamp
InvalidValue*/
