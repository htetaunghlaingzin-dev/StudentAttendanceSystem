using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.RazorPages;
namespace Attendly.Web.Pages;
public class IndexModel(IHttpClientFactory clients):PageModel
{
    public string ApiStatus{get;private set;}="Checking API...";
    public async Task OnGetAsync(){try{var result=await clients.CreateClient("AttendlyApi").GetFromJsonAsync<Health>("/api/health");ApiStatus=result?.Status??"Unavailable";}catch{ApiStatus="API is not running";}}
    sealed record Health(string Status,string Service);
}
