#r "System.Net.Http"
// #r "Microsoft.VisualStudio.Services.Common"

using System;
using System.Linq;
using System.Net;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.VisualStudio.Services.Common;
using Microsoft.VisualStudio.Services.Client;
using Microsoft.TeamFoundation.SourceControl.WebApi;
using Microsoft.VisualStudio.Services.WebApi;
using Microsoft.TeamFoundation.Core.WebApi;
using Microsoft.VisualStudio.Services.Identity.Client;



static IEnumerable<TeamProjectReference> GetProjects(VssConnection Connection)
{
    var projectClient = Connection.GetClient<ProjectHttpClient>();
    return projectClient.GetProjects().Result;
}

static string GetProjectId(VssConnection Connection, string ProjectName)
{
    var allProjects = GetProjects(Connection);
    var project = allProjects.FirstOrDefault(proj => proj.Name == ProjectName);
    return project.Id.ToString();
}

static Task<TeamProject> GetProjectAsync(VssConnection Connection, string projectId)
{
    var projectClient = Connection.GetClient<ProjectHttpClient>();

    return projectClient.GetProject(projectId);
}

static async Task<List<string>> GetTeamMembers(VssConnection Connection, string projectId)
{
    var project = await GetProjectAsync(Connection, projectId);
    var client = Connection.GetClient<TeamHttpClient>();
    var groupIdentity = await client.GetTeamMembers(projectId, project.DefaultTeam.Id.ToString());

    var Users = groupIdentity.ToList().Select(q => q.UniqueName);
    return Users.ToList();
}

static async Task<string> GrantProjectAccessAsync(VssConnection Connection, string projectId, string email)
{
    var project = await GetProjectAsync(Connection, projectId);
    var client = Connection.GetClient<IdentityHttpClient>();
    var identities = await client.ReadIdentitiesAsync(Microsoft.VisualStudio.Services.Identity.IdentitySearchFilter.MailAddress, email);
    if (!identities.Any() || identities.Count > 1)
    {
        throw new InvalidOperationException("User not found or could not get an exact match based on email");
    }
    var userIdentity = identities.Single();

    Console.WriteLine(userIdentity.DisplayName);
    var groupIdentity = await client.ReadIdentityAsync(project.DefaultTeam.Id);

    var FinalAct = await client.AddMemberToGroupAsync(groupIdentity.Descriptor, userIdentity.Id);

    return userIdentity.DisplayName;

}

static async Task<string> RevokeProjectAccessAsync(VssConnection Connection, string projectId, string email)
{
    var project = await GetProjectAsync(Connection, projectId);
    var client = Connection.GetClient<IdentityHttpClient>();
    var identities = await client.ReadIdentitiesAsync(Microsoft.VisualStudio.Services.Identity.IdentitySearchFilter.MailAddress, email);
    if (!identities.Any() || identities.Count > 1)
    {
        throw new InvalidOperationException("User not found or could not get an exact match based on email");
    }
    var userIdentity = identities.Single();

    Console.WriteLine(userIdentity.DisplayName);
    var groupIdentity = await client.ReadIdentityAsync(project.DefaultTeam.Id);

    var FinalAct = await client.RemoveMemberFromGroupAsync(groupIdentity.Descriptor, userIdentity.Descriptor);

    return userIdentity.DisplayName;

}

public static async Task<HttpResponseMessage> Run(HttpRequestMessage req, TraceWriter log)
{
    dynamic data = await req.Content.ReadAsAsync<object>();
    string Project = data?.Project;
    string Email = data?.Email;
	string AccName = "YOUR VSTS ACCOUNT NAME"
	
    string collectionUri = $"https://{AccName}.visualstudio.com/DefaultCollection";
    string ProjectName = Project;
    VssCredentials creds = new VssBasicCredential(string.Empty, "YOUR VSTS TOKEN");
    VssConnection VstsConnection = new VssConnection(new Uri(collectionUri), creds);

    log.Info("C# HTTP trigger function processed a request.");

    
    

    string ProjectId = GetProjectId(VstsConnection, ProjectName);
    var res = GrantProjectAccessAsync(VstsConnection, ProjectId, Email);
    // var Result = "str";

    log.Info(Project);
    log.Info(Email);
    log.Info(ProjectId);

    return Email == null
        ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a Email on the query string or in the request body")
        : req.CreateResponse(HttpStatusCode.OK, "Hello " + Email);
}
