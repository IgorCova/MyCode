using System;
using System.IO;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Ply.app;

namespace Ply.Controllers {
  #region Controller
  public class Api<T> : Controller where T : class, new() {
    private ILoggerFactory loggerFactory;
    private ILogger logger;
    public T response = new T();
    public int Executer;
    public string resp = "";
    public string answer = "";
    public string body = "";
    public string message = "Ok";

    public Api() {
      loggerFactory = new LoggerFactory()
        .AddConsole()
        .AddDebug();

      loggerFactory.AddFile(Path.Combine(Directory.GetCurrentDirectory(), "log", "api.out"));
      logger = loggerFactory.CreateLogger("ApiLogger");
    }

    internal void Log(string message) {
      logger.LogError($"{DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss")}: {message}".Replace("  ", " "));
    }

    public void Decode(HttpRequest request) {
      string token = Request.Headers["Authorization"];
      if (string.IsNullOrEmpty(token)) {
        HttpContext.Response.StatusCode = 401;
        throw new ApiException("No Authorization header");
      }

      try {
        JwtDecoder decoder = Data.JwtDecode(token);

        Executer = decoder.Id_User;
      } catch (InvalidToken it) {
        HttpContext.Response.StatusCode = 401;
        throw new ApiException(it.Message);
      } catch (ExpiredToken et) {
        HttpContext.Response.StatusCode = 401;
        throw new ApiException(et.Message);
      } catch (Exception e) {
        Console.Out.WriteLine(e.Message);
        HttpContext.Response.StatusCode = 400;
        throw new ApiException("Unhandeled exception");
      }
    }
  }
  #endregion Controller

  #region Check
  [Route("check")]
  public sealed class CheckController : Controller {
    [HttpGet]
    public string Get() {
      string resp = "Success";

      try {
        Data.Check();
      } catch (Exception e) {
        resp = string.Format("Failed: ", e.Message);
      }

      return resp;
    }
  }
  #endregion Check

  #region Login
  [Route("login")]
  public sealed class LoginController : Api<SimpleResponse> {
    [HttpGet]
    public SimpleResponse Get() {
      try {
        string user = Request.Headers["User"];
        string pass = Request.Headers["Pass"];
        if (string.IsNullOrEmpty(user)) {
          HttpContext.Response.StatusCode = 400;
          throw new ApiException("No User header");
        }
        if (string.IsNullOrEmpty(pass)) {
          HttpContext.Response.StatusCode = 400;
          throw new ApiException("No Pass header");
        }
        response = Data.Login(user, pass);
      } catch (ApiException ae) {
        message = ae.Message;
        response.Result = false;
        response.Detail = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = response.Result.ToString();
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("profile")]
  public sealed class ProfileController : Api<ProfileResponse> {
    [HttpGet]
    public ProfileResponse Get([FromQuery] int? user) {
      try {
        Decode(Request);
        response.Data = Data.User_Read(user ?? Executer);
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }
  #endregion Login

  #region Client
  [Route("clients")]
  public sealed class ClientController : Api<ClientListResponse> {
    [HttpGet]
    public ClientListResponse Get([FromQuery] string closed) {
      try {
        Decode(Request);
        response = Data.Client_List(Executer, closed == "yes" ? true : false);
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("client/save")]
  public sealed class ClientSaveController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] Client client) {
      try {
        Decode(Request);
        response.Data = Data.Client_Save(Executer, client);
        response.Result = true;
        body = JsonConvert.SerializeObject(client);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("client/close")]
  public sealed class ClientCloseController : Api<SimpleResponse> {
    [HttpGet]
    public SimpleResponse Get([FromQuery] int client, string closed) {
      try {
        Decode(Request);
        response.Data = Data.Client_Close(Executer, client, closed == "yes" ? true : false);
        response.Result = true;
        body = JsonConvert.SerializeObject(client);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("client/drop")]
  public sealed class ClientDropController : Api<SimpleResponse> {
    [HttpGet]
    public SimpleResponse Get([FromQuery] int client) {
      try {
        Decode(Request);
        response.Data = Data.Client_Delete(Executer, client);
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }
  #endregion Client

  #region Project
  [Route("projects")]
  public sealed class ProjectController : Api<ProjectListResponse> {
    [HttpGet]
    public ProjectListResponse Get([FromQuery] int client, string closed) {
      try {
        Decode(Request);
        response.Data = Data.Project_List(Executer, client, closed == "yes" ? true : false);
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("project/save")]
  public sealed class ProjectSaveController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] Project project) {
      try {
        Decode(Request);
        response.Data = Data.Project_Save(Executer, project);
        response.Result = true;
        body = JsonConvert.SerializeObject(project);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("project/team")]
  public sealed class ProjectTeamController : Api<Project_TeamListResponse> {
    [HttpGet]
    public Project_TeamListResponse Get([FromQuery] int project) {
      try {
        Decode(Request);
        response.Data = Data.Project_Team(project);
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("project/close")]
  public sealed class ProjectCloseController : Api<SimpleResponse> {
    [HttpGet]
    public SimpleResponse Get([FromQuery] int project, string closed) {
      try {
        Decode(Request);
        response.Data = Data.Project_Close(Executer, project, closed == "yes" ? true : false);
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("project/drop")]
  public sealed class ProjectDropController : Api<SimpleResponse> {
    [HttpGet]
    public SimpleResponse Get([FromQuery] int project) {
      try {
        Decode(Request);
        response.Data = Data.Project_Delete(Executer, project);
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }
  #endregion Project

  #region Expense
  [Route("expenses")]
  public sealed class ExpenseController : Api<ExpenseListResponse> {
    [HttpGet]
    public ExpenseListResponse Get([FromQuery] int project) {
      try {
        Decode(Request);
        response.Data = Data.Expense_List(Executer, project);
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("expense/save")]
  public sealed class ExpenseSaveController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] ExpenseSave expense) {
      try {
        Decode(Request);
        response.Data = Data.Expense_Save(Executer, expense);
        response.Result = true;
        body = JsonConvert.SerializeObject(expense);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        Console.Out.WriteLine(e.Message);
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("expense/set_status")]
  public sealed class ExpenseSetStatusController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] ExpenseSetStatus expense) {
      try {
        Decode(Request);
        response.Data = Data.Expense_SetStatus(Executer, expense);
        response.Result = true;
        body = JsonConvert.SerializeObject(expense);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        Console.Out.WriteLine(e.Message);
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }
  #endregion Expense

  #region Comment
  [Route("comments")]
  public sealed class CommentController : Api<CommentListResponse> {
    [HttpGet]
    public CommentListResponse Get([FromQuery] int project) {
      try {
        Decode(Request);
        response = Data.Comment_List(Executer, project);
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("comment/save")]
  public sealed class CommentSaveController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] CommentSave comment) {
      try {
        Decode(Request);
        response.Data = Data.Comment_Save(Executer, comment);
        response.Result = true;
        body = JsonConvert.SerializeObject(comment);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        Console.Out.WriteLine(e.Message);
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("comment/drop")]
  public sealed class CommentDropController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] CommentItem comment) {
      try {
        Decode(Request);
        response.Data = Data.Comment_Drop(Executer, comment);
        response.Result = true;
        body = JsonConvert.SerializeObject(comment);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        Console.Out.WriteLine(e.Message);
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }
  #endregion Comment

  #region User
  [Route("users")]
  public sealed class UserController : Api<UserListResponse> {
    [HttpGet]
    public UserListResponse Get([FromQuery] int role) {
      try {
        Decode(Request);
        response.Data = Data.User_List(role);
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("user/save")]
  public sealed class UserSaveController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] UserSave user) {
      try {
        Decode(Request);
        user.Check();
        response.Data = Data.User_Save(Executer, user);
        response.Result = true;
        user.Pass = "****";
        body = JsonConvert.SerializeObject(user);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("user/drop")]
  public sealed class UserDropController : Api<SimpleResponse> {
    [HttpGet]
    public SimpleResponse Get([FromQuery] int user) {
      try {
        Decode(Request);
        response.Data = Data.User_Drop(Executer, user);
        response.Result = true;
        body = JsonConvert.SerializeObject(user);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("user/pass")]
  public sealed class UserPassController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] UserPass user) {
      try {
        Decode(Request);
        response.Data = Data.User_SetPass(Executer, user);
        response.Result = true;
        user.Pass = "****";
        body = JsonConvert.SerializeObject(user);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }
  #endregion User

  #region Role
  [Route("roles")]
  public sealed class RoleController : Api<RoleListResponse> {
    [HttpGet]
    public RoleListResponse Get() {
      try {
        Decode(Request);
        response.Data = Data.Role_List();
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }
  #endregion Role

  #region Team
  [Route("team/add")]
  public sealed class TeamAddController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] Team team) {
      try {
        Decode(Request);
        response.Data = Data.Team_AddMember(Executer, team);
        response.Result = true;
        body = JsonConvert.SerializeObject(team);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("team/drop")]
  public sealed class TeamDropController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] Team team) {
      try {
        Decode(Request);
        response.Data = Data.Team_DropMember(Executer, team);
        response.Result = true;
        body = JsonConvert.SerializeObject(team);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("team/save")]
  public sealed class TeamSaveController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] TeamMembers team) {
      try {
        Decode(Request);
        response.Data = Data.Team_Save(Executer, team);
        response.Result = true;
        body = JsonConvert.SerializeObject(team);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }
  #endregion Team

  #region Category
  [Route("categories")]
  public sealed class CategoryController : Api<CategoryListResponse> {
    [HttpGet]
    public CategoryListResponse Get([FromQuery] string closed) {
      try {
        Decode(Request);
        response.Data = Data.Category_List(closed == "yes" ? true : false);
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }

  [Route("category/save")]
  public sealed class CategorySaveController : Api<SimpleResponse> {
    [HttpPost]
    public SimpleResponse Get([FromBody] CategorySave category) {
      try {
        Decode(Request);
        response.Data = Data.Category_Save(Executer, category);
        response.Result = true;
        body = JsonConvert.SerializeObject(category);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }
  #endregion Category

  #region Status
  [Route("statuses")]
  public sealed class StatusController : Api<StatusListResponse> {
    [HttpGet]
    public StatusListResponse Get() {
      try {
        Decode(Request);
        response.Data = Data.Status_List();
        response.Result = true;
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        answer = JsonConvert.SerializeObject(response);
        Data.ApiRequest_Save(Executer, null, HttpContext.Request.Path.Value, answer, message);
      }

      return response;
    }
  }
  #endregion Status

  #region Log
  [Route("logs")]
  public sealed class LogsController : Api<LogListResponse> {
    [HttpPost]
    public LogListResponse Get([FromBody] LogRequest reuest) {
      try {
        Decode(Request);
        response.Data = Data.Log_List(Executer, reuest);
        response.Result = true;
        body = JsonConvert.SerializeObject(reuest);
      } catch (ApiException ae) {
        response.Result = false;
        response.Detail = ae.Message;
        message = ae.Message;
      } catch (Exception e) {
        Log($"{e.StackTrace}: {e.Message}");
        message = e.Message;
        response.Result = false;
      } finally {
        //answer = JsonConvert.SerializeObject(response); // answer to long
        Data.ApiRequest_Save(Executer, body, HttpContext.Request.Path.Value, null, message);
      }

      return response;
    }
  }
  #endregion Log
}