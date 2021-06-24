# ApprovalTestsExtensions
A set of helper classes that facilitate the usage of [ApprovalTests.NET library](https://github.com/approvals/approvaltests.net).


## How to install
`ApprovalTestsExtensions` is distribute as a nuget package [SmartAnalyzers.ApprovalTestsExtensions](https://www.nuget.org/packages/SmartAnalyzers.ApprovalTestsExtensions/)

## What problems does it solve?

1. Approvals.Net doesn't work correctly inside the `async` methods called from the test method. `ApprovalTestsExtensions` solves this problem by providing a custom implementation of `IApprovalNamer` which takes the leverage of `[CallerFilePath]` and `[CallerMemberName]` attributes. Required parts for a snapshot file name are resolved during the compilation hence there is no need to analyze the stack trace anymore.

2. Approvals.Net doesn't provide a way to ignore some parts of the asserted payload. `ApprovalTestsExtensions` allows to specify ignored attributes for JSON payload with [JsonPath](https://github.com/json-path/JsonPath).

3. Approvals.Net doesn't have an out-of-the-box mechanism for bulk updates of the existing snapshots. `ApprovalTestsExtensions` provides a custom implementation `IReporterWithApprovalPower` which allows to perform such operation.

4. Calling Approvals multiple times within a single method without scenario name or with the same scenario results with snapshot overrides. This is highly undesirable and quite often caused by copy&paste mistakes. `ApprovalTestsExtensions` tracks snapshot names per instance and prevents overrides by throwing `SnapshotOverriddenException` in such a case.

## How to use it

Just create an instance of `SmartAnalyzers.ApprovalTestsExtensions.ExplicitApprover` class and start using it for approving your snapshots. The `ExplicitApprover` class offers the following helper methods:

- `VerifyHttpResponse` - Checking the returned JSON payload with saved snapshot.
- `VerifyHttpResponseForScenario` - Same as above but allows to specify scenario name. Helpful when there is more than one snapshot inside a single test.
- `VerifyJson` - Checking the provided explicitly JSON payload with a saved snapshot.
- `VerifyJsonForScenario` - Same as above but allows specifying scenario name. Helpful when there is more than one snapshot inside a single test.
- `VerifyJsonDiff` - Calculate the diff between the provided JSON payloads and checking the result against the saved snapshot. More info about the diff can be found on [jsondiffpatch.net]( https://github.com/wbish/jsondiffpatch.net) project site. Similar method available for `HttpResponseMessage`.

All those methods allow for ignoring attributes inside the payload by specifying them as `ignoredPaths` in the form of [JsonPath](https://github.com/json-path/JsonPath). This is especially useful when the payload contains dynamically generated data like dates or identifiers.

**IMPORTANT**: `ExplicitApprover` is memorizing test method name during the constructor invocation so it should be created per every test method. You should not re-use `ExplicitApprover` instance between test methods as it will result in incorrect snapshot names. If you want to extract `ExplicitApprover` instance creation to the method then all constructor parameters should be passed explicitly.

Example test can look as follows:

```cs
[Test]
public async Task should_get_details_for_newly_registered_user()
{
    // STEP 1: Create a new instance of the ExplicitApprover
    var approver = new ExplicitApprover();
    
    // STEP 2: Create API client for our Service
    var applicationFactory = new SampleApplicationFactory();
    var apiClient = applicationFactory.CreateClient();

    // STEP 3: Call register user endpoint
    var registerUserResponse = await apiClient.PostAsJsonAsync(new {
        Login = "JohnDoe",
        Email = "john@doe.com",
        Password = "Secret#1234"
    });

    // STEP 4: Verify the response snapshot
    await approver.VerifyHttpResponseForScenario("RegisterUserResponse", registerUserResponse);
    
    // STEP 5: Call the user details endpoint
    var registerResult = await registerUserResponse.Content.ReadAsAsync<RegisterUserResult>();
    var userDetailsResponse = await apiClient.GetAsync("/user/" + registerResult.Id);

    // STEP 6: Verify the response snapshot ignoring the id property inside the payload
    await approver.VerifyHttpResponseForScenario("NewUserDetails", userDetailsResponse, new []{"$.id"});
}
```

## Bulk snapshot update

To perform an update of all existing snapshots just set the static property `ExplicitApprover.UseAutoApprover` to true or specify it during the `ExplicitApprover` instance creation:

```cs
public async Task sample_test()
{
    // STEP 1: Create a new instance of the ExplicitApprover with `useAutoApprover`
    var approver = new ExplicitApprover(useAutoApprover: true);
}
```