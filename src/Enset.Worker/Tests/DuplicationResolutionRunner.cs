    
    /*private static void RunCustomerDuplicationResolution(
        List<CustomerImportDto> customers)
    {
        Console.WriteLine();
        Console.WriteLine("========== DUPLICATION CHECK ==========");

        var validator = new CustomerDuplicateValidator();

        var duplicateCandidates = validator.FindDuplicates(customers);

        Console.WriteLine($"Dubletten gefunden: {duplicateCandidates.Count}");

        if (duplicateCandidates.Count == 0)
        {
            return;
        }

        var issues = duplicateCandidates
            .Select(CustomerDuplicateIssueMapper.ToIssue)
            .ToList();

        var resolutionService = new ConsoleImportIssueResolutionService();

        var resolutionCompleted = resolutionService.ResolveIssues(issues);

        if (!resolutionCompleted)
        {
            Console.WriteLine("Import wurde durch Benutzer abgebrochen.");
            return;
        }

        var resultBuilder = new ImportResolutionResultBuilder();
        var mergeInstructionBuilder = new CustomerMergeInstructionBuilder();

        var customerMergeInstructions = new List<CustomerMergeInstruction>();

        for (var i = 0; i < duplicateCandidates.Count; i++)
        {
            var issue = issues[i];

            if (!issue.IsResolved)
            {
                continue;
            }

            var result = resultBuilder.Build(issue);

            var instruction = mergeInstructionBuilder.Build(
                duplicateCandidates[i],
                result);

            if (instruction is not null)
            {
                customerMergeInstructions.Add(instruction);
            }
        }

    var groupBuilder = new CustomerMergeGroupBuilder();

    var customerMergeGroups = groupBuilder.Build(customerMergeInstructions);

    Console.WriteLine();
    Console.WriteLine($"Customer Merge Groups: {customerMergeGroups.Count}");

    foreach (var group in customerMergeGroups)
    {
        Console.WriteLine("--------------------------------");
        Console.WriteLine($"Master: {group.MasterCustomer.CompanyName}");
        Console.WriteLine($"Name:   {group.ResolvedName}");

        Console.WriteLine("Duplicates:");

        foreach (var duplicate in group.DuplicateCustomers)
        {
            Console.WriteLine($" - {duplicate.CompanyName}");
        }
    }
    }*/