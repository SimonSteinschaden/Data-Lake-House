using Microsoft.AspNetCore.Mvc;

namespace Enset.Api.Errors;

public static class ApiProblems
{
    public static ObjectResult InvalidImportRequest(ControllerBase controller, string detail)
    {
        return controller.Problem(
            title: "Invalid import request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);
    }

    public static ObjectResult InvalidResolutionRequest(ControllerBase controller, string detail)
    {
        return controller.Problem(
            title: "Invalid resolution request",
            detail: detail,
            statusCode: StatusCodes.Status400BadRequest);
    }

    public static ObjectResult ImportNotFound(ControllerBase controller, Guid importId)
    {
        return controller.Problem(
            title: "Import report not found",
            detail: $"Import report '{importId}' was not found.",
            statusCode: StatusCodes.Status404NotFound);
    }

    public static ObjectResult ImportConflict(ControllerBase controller, string detail)
    {
        return controller.Problem(
            title: "Import conflict",
            detail: detail,
            statusCode: StatusCodes.Status409Conflict);
    }
}