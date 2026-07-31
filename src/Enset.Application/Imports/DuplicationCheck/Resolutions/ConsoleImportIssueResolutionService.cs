using Enset.Application.Imports.Issues;

namespace Enset.Application.Imports.Resolution;

public class ConsoleImportIssueResolutionService
{
    public bool ResolveIssues(IEnumerable<ImportIssue> issues)
    {
        foreach (var issue in issues.Where(x => x.RequiresUserDecision && !x.IsResolved))
        {
            Console.WriteLine();
            Console.WriteLine("Import-Konflikt gefunden");
            Console.WriteLine("-----------------------");
            Console.WriteLine($"Typ: {issue.Type}");
            Console.WriteLine($"Feld: {issue.FieldName}");
            Console.WriteLine($"Erste Variante:  {issue.FirstValue}");
            Console.WriteLine($"Zweite Variante: {issue.SecondValue}");
            Console.WriteLine($"Ähnlichkeit: {issue.SimilarityScore:P1}");
            Console.WriteLine($"Grund: {issue.Message}");
            Console.WriteLine();

            Console.WriteLine("Bitte Entscheidung wählen:");
            Console.WriteLine("1 = Erste Variante behalten");
            Console.WriteLine("2 = Zweite Variante behalten");
            Console.WriteLine("3 = Eigene Bezeichnung eingeben");
            Console.WriteLine("4 = Getrennt lassen");
            Console.WriteLine("X = Importprozess abbrechen");

            var input = Console.ReadLine();

            switch (input)
            {
                case "1":
                    issue.ResolveManually(
                        ImportResolutionAction.KeepFirst,
                        null,
                        "console-user",
                        DateTime.UtcNow);
                    break;

                case "2":
                    issue.ResolveManually(
                        ImportResolutionAction.KeepSecond,
                        null,
                        "console-user",
                        DateTime.UtcNow);
                    break;

                case "3":
                    Console.Write("Eigene Bezeichnung: ");
                    issue.ResolveManually(
                        ImportResolutionAction.UseCustomValue,
                        Console.ReadLine(),
                        "console-user",
                        DateTime.UtcNow);
                    break;

                case "4":
                    issue.ResolveManually(
                        ImportResolutionAction.KeepSeparate,
                        null,
                        "console-user",
                        DateTime.UtcNow);
                    break;

                case "x":
                case "X":
                    return false;

                default:
                    Console.WriteLine("Ungültige Eingabe. Issue bleibt ungelöst.");
                    break;
            }
        }

        return true;
    }
}
