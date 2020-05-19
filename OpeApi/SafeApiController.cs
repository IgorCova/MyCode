using Api.Classes;
using Api.Extensions;
using Api.Interfaces;
using Api.Models;
using Api.Tools;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Newtonsoft.Json;
using System;
using System.Text;
using System.Threading.Tasks;
using DA = Api.DataAccess;

namespace Api.Controllers {
  #region SafeApi Controller
  public class SafeApi<R> : ControllerBase where R : IRequest, new() {
    public SafeResponse apiResponse = Builder.Build_SafeResponse();
    public ApiUser apiUser = Builder.Build_ApiUser();
    public IMemoryCache memoryCache;
    internal IChainLogger chainLogger = Builder.Build_ChainLogger();
    internal R apiRequest = new R();
    internal ApiRequest request = new ApiRequest();
    internal string response;
    internal string username;

    internal SafeApi(IMemoryCache cache) {
      memoryCache = cache;
      DataCache.SetMemoryCache(cache);
    }

    #region Save_Request
    internal void Save_Request() {
      apiRequest.Save();
      chainLogger.Step(Tool.GetCurrentMethod());
    }
    #endregion Save_Request

    #region Review_HttpRequest
    internal virtual void Review_HttpRequest(HttpRequest httpRequest) {
      try {
        request.Path = httpRequest.Path.Value;
        request.Method = httpRequest.Method;
        request.Version = httpRequest.GetHeaderValue("api-version").ToDecimal("api_version");
        request.Date = chainLogger.Start(request);

        username = Tool.Get_UsernameFromToken(httpRequest.GetHeaderValue("Authorization"));
        chainLogger.Step("Token_Decode");
        chainLogger.SetUsername(username);

        ApiUserCache.Get_UserData(username, ref apiUser);

        if (string.IsNullOrEmpty(apiUser.PublicKey)) {
          Console.Out.WriteLine("PublicKey not found");
          throw new ApiException(CodeStatus.Permission_Denied);
        }

        chainLogger.Step("Get_UserData");
      } catch (ApiException ae) {
        throw ae;
      } catch (Exception e) {
        Print(e, Tool.GetCurrentMethod());
        throw new ApiException("Authorization failed");
      }
    }
    #endregion Review_HttpRequest

    #region Access_Authorization
    internal void Access_Authorization() {
      bool result = apiRequest.Authorization();
      chainLogger.Step(Tool.GetCurrentMethod());
      if (result == false) {
        throw new ApiException(CodeStatus.Permission_Denied);
      }
    }
    #endregion Access_Authorization

    #region Encrypt_Data
    internal SafeData Encrypt_Data<DD>(DD decryted_data) where DD : class {
      TripleDESHelper des = new TripleDESHelper();

      using (RSAHelper rsa_partner = new RSAHelper(RSAType.RSA2, Encoding.UTF8, Globals.key_private, apiUser.PublicKey)) {
        response = JsonConvert.SerializeObject(decryted_data);
#if DEBUG
        Console.Out.WriteLine(response);
#endif
        string encryptedStr = des.Encrypt(response);

        string desPrms = des.GetParameters();
        string desEncrypted = rsa_partner.Encrypt(desPrms);
        string signStr = rsa_partner.Sign(desPrms);
        bool signVerify = false;

        using (RSAHelper rsa = new RSAHelper(RSAType.RSA2, Encoding.UTF8, Globals.key_private, Globals.key_public)) {
          signVerify = rsa.Verify(desPrms, signStr);
        }

        using (SafeData sd = new SafeData()) {
          sd.Data = encryptedStr;
          sd.Signature = signStr;
          sd.Des = desEncrypted;

          chainLogger.Step(Tool.GetCurrentMethod());
          return sd;
        }
      }
    }
    #endregion Encrypt_Data

    #region Decrypt_Data
    internal void Decrypt_Data<ED>(ref ED encrytedData, ref SafeData safeData) where ED : IRequest {
      string desDecrypted = string.Empty;
      using (RSAHelper rsa = new RSAHelper(RSAType.RSA2, Encoding.UTF8, Globals.key_private, Globals.key_public)) {
        desDecrypted = rsa.Decrypt(safeData.Des);
      }

      using (RSAHelper rsa_partner = new RSAHelper(RSAType.RSA2, Encoding.UTF8, Globals.key_private, apiUser.PublicKey)) {
        if (rsa_partner.Verify(desDecrypted, safeData.Signature) == false) {
          throw new ApiException(CodeStatus.Signature_Not_Valid);
        }
      }

      using (DESParameters desParameters = JsonConvert.DeserializeObject<DESParameters>(desDecrypted)) {
        TripleDESHelper des = new TripleDESHelper(desParameters);
        string message = des.Decrypt(safeData.Data);
        encrytedData = JsonConvert.DeserializeObject<ED>(message);
        request.User_ID = apiUser.User_ID;
        encrytedData.SetBase(request);
      }

      chainLogger.Step(Tool.GetCurrentMethod());
      Access_Authorization();
      Save_Request();
    }
    #endregion Decrypt_Data

    #region Save_Response
    internal void Save_Response() {
      try {
        DA.Response_Save(apiRequest, response, apiResponse.Status);
      } catch (Exception e) {
        Print(e, Tool.GetCurrentMethod());
      }
    }
    #endregion Save_Response

    #region Print
    internal void Print(Exception exception, string method) {
      Print(exception, method, false);
      Print("StackTrace", method, exception.StackTrace);
    }

    internal void Print(Exception exception, string method, bool isSafe) {
      Print("Exception", method, exception.Message);
      Print("StackTrace", method, exception.StackTrace);
    }

    internal void Print(ApiException level, string Method) {
      Print("Warning", Method, level.Message);
    }

    internal void Print(string Level, string Method, string Message) {
      Console.Out.WriteLine($"[{username}] {Level}: {Method} => {Message}");
    }
    #endregion Print
  }
  #endregion SafeApi Controller

  #region check
  [ApiVersion("2.0")]
  [Route("check")]
  [BodyRewind]
  public sealed class CheckController : SafeApi<SafeApiRequest> {
    public CheckController(IMemoryCache cache) : base(cache) { }
    [HttpPost]
    public SafeResponse Check([FromBody] SimpleData simpleData) {
      try {
        DA.Check();

        if ((simpleData != null) && (!string.IsNullOrEmpty(simpleData.Data))) {
          apiUser = new ApiUser() {
            User_ID = 0,
            PublicKey = string.IsNullOrEmpty(simpleData.PublicKey) ? Globals.key_public : simpleData.PublicKey
          };
          using (SafeData sd = Encrypt_Data(simpleData.Data)) {
            apiResponse = new SafeResponse(sd, CodeStatus.Ok);
          }
        } else {
          apiResponse = new SafeResponse(CodeStatus.Ok);
        }
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
        HttpContext.Response.StatusCode = 204;  // No content - no connection to DataBase
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
        HttpContext.Response.StatusCode = 418; // I'm teapot
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Route("health/{probe}")]
  public sealed class HealthController : SafeApi<SafeApiRequest> {
    public HealthController(IMemoryCache cache) : base(cache) { }
    [HttpGet]
    public SafeResponse Check(string probe = "health") {
      try {
        DA.Check();
        apiResponse = new SafeResponse(CodeStatus.Ok);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
        HttpContext.Response.StatusCode = 500;  // No content - no connection to DataBase
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
        HttpContext.Response.StatusCode = 418; // I'm teapot
      }

      Console.Out.WriteLine($"Probe {probe}: {apiResponse.Status.Detail}");

      return apiResponse;
    }
  }
  #endregion Check

  #region cards
  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("cards")]
  public sealed class CardsController : SafeApi<Request_Cards> {
    public CardsController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_Cards([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        using (AutoDisposeList<Card> cards = DA.Get_Cards(ref apiRequest)) {
          chainLogger.Database_Request();
          using (SafeData sd = Encrypt_Data(cards)) {
            apiResponse = new SafeResponse(sd, CodeStatus.Ok);
          }
        }
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }
  #endregion cards

  #region card
  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card")]
  public sealed class CardController : SafeApi<Request_Card> {
    public CardController(IMemoryCache cache) : base(cache) { }

    [HttpPost("info")]
    public SafeResponse Post_CardInfo([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        CardInfo data = DA.Get_Card_Info(ref apiRequest);
        chainLogger.Database_Request();

        SafeData sd = Encrypt_Data(data);

        apiResponse = new SafeResponse(sd, CodeStatus.Ok);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }

    [HttpPost("info/joint")]
    public SafeResponse Post_CardInfoJoint([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        CardInfo data = DA.Get_Card_Info_Joint(ref apiRequest);
        chainLogger.Database_Request();

        SafeData sd = Encrypt_Data(data);

        apiResponse = new SafeResponse(sd, CodeStatus.Ok);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }

    [HttpPost("reset_pin_counter")]
    public SafeResponse Post_CardPinCountReset([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        bool data = DA.Reset_Pin_Counter(ref apiRequest);
        chainLogger.Database_Request();

        apiResponse = new SafeResponse((data == true) ? CodeStatus.Ok : CodeStatus.Warning);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        Task.Run(() => Save_Response());
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }

    [HttpPost("cvc2")]
    public SafeResponse Card_Cvc2([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        CardCvc2 data = DA.Get_CardCVC2(ref apiRequest);

        SafeData sd = Encrypt_Data(data);

        apiResponse = new SafeResponse(sd, (data.Cvc2.Length == 3) ? CodeStatus.Ok : CodeStatus.Warning);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/virtual")]
  public sealed class CardvirtualController : SafeApi<Request_VirtualCard> {
    public CardvirtualController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_CardVirtual([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        Tool.ValidatePhoneNumber(apiRequest.Phone_No);
        apiRequest.Exp_Date = Tool.ValidateExpirationDate(apiRequest.Exp_Date);
        apiRequest.Informing = apiRequest.Informing.ToUpper();
        Tool.Validate_Status(apiRequest.Informing, "Informing");

        VirtualCard vcard = new VirtualCard(ref apiRequest);

        VirtualCardOut data = DA.Create_VirtualCard(ref apiRequest, ref vcard);

        SafeData sd = Encrypt_Data(data);
        apiResponse = new SafeResponse(sd, CodeStatus.Ok);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod(), false);
      } finally {
        Task.Run(() => Save_Response());
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/sms")]
  public sealed class CardSmsController : SafeApi<Request_CardSms> {
    public CardSmsController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_CardSms([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        apiRequest.Validate_Status();

        bool data = DA.Sms_Informing(ref apiRequest);

        apiResponse = new SafeResponse((data == true) ? CodeStatus.Ok : CodeStatus.Warning);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        Task.Run(() => Save_Response());
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/set_phone")]
  public sealed class CardSetPhoneController : SafeApi<Request_SetPhone> {
    public CardSetPhoneController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_CardSetPhone([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        apiRequest.Validate_Phone();
        bool data = DA.Set_Phone(ref apiRequest);

        apiResponse = new SafeResponse((data == true) ? CodeStatus.Ok : CodeStatus.Warning);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod(), false);
      } finally {
        Task.Run(() => Save_Response());
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/block")]
  public sealed class CardBlockController : SafeApi<Request_BlockCard> {
    public CardBlockController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_CardBlock([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);
        apiRequest.Validate_Status();

        bool data = false;
        if (apiRequest.Status == "ON") {
          data = DA.Block_Card(ref apiRequest);
        } else {
          data = DA.UnBlock_Card(ref apiRequest);
        }

        apiResponse = new SafeResponse((data == true) ? CodeStatus.Ok : CodeStatus.Warning);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        Task.Run(() => Save_Response());
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/set_pin")]
  public sealed class CardSetPinController : SafeApi<Request_SetPin> {
    public CardSetPinController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_CardSetPin([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        bool data = DA.Set_Pin(ref apiRequest);

        apiResponse = new SafeResponse((data == true) ? CodeStatus.Ok : CodeStatus.Warning);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod(), false);
      } finally {
        Task.Run(() => Save_Response());
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/personification")]
  public sealed class CardPersonificationController : SafeApi<SafeApiRequest> {
    public CardPersonificationController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_CardPersonification([FromBody] SafeData safeData) {
      try {
        throw new ApiException(CodeStatus.Not_Allowed_Method);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod(), false);
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/limit")]
  public sealed class CardLimitController : SafeApi<Request_SetLimit> {
    public CardLimitController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_CardLimit([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        bool data = DA.Set_CardLimit(ref apiRequest);

        apiResponse = new SafeResponse((data == true) ? CodeStatus.Ok : CodeStatus.Warning);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        Task.Run(() => Save_Response());
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/limit")]
  public sealed class CardDelLimitController : SafeApi<Request_ClearLimit> {
    public CardDelLimitController(IMemoryCache cache) : base(cache) { }

    [HttpDelete]
    public SafeResponse Delete_CardLimit([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        bool data = DA.Clear_CardLimit(ref apiRequest);

        apiResponse = new SafeResponse((data == true) ? CodeStatus.Ok : CodeStatus.Warning);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        Task.Run(() => Save_Response());
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/detail/inform")]
  public sealed class Card_DetailInform_Controller : SafeApi<Request_CardDetail> {
    public Card_DetailInform_Controller(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Card_DetailInform([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);
        bool data = DA.CardDetail_Inform(ref apiRequest);

        apiResponse = new SafeResponse((data == true) ? CodeStatus.Ok : CodeStatus.Warning);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }
  #endregion card

  #region card/transactions
  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/transactions")]
  public sealed class Post_CardTransactions : SafeApi<Request_Transactions> {
    public Post_CardTransactions(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Get_Transactions([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        using (TransactionPage data = DA.Get_Transactions(ref apiRequest)) {
          chainLogger.Database_Request();
          using (SafeData sd = Encrypt_Data(data)) {
            apiResponse = new SafeResponse(sd, CodeStatus.Ok);
          }
        }
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/transactions/top")]
  public sealed class Post_CardTopTransactions : SafeApi<Request_TopTransactions> {
    public Post_CardTopTransactions(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Get_TopTransactions([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        if ((apiRequest.Top < 1) || ((apiRequest.Top > 99))) {
          throw new ApiException("Invalid Top number. Must be from 1 to 99");
        }

        using (AutoDisposeList<Transaction> data = DA.Get_Transactions(ref apiRequest).Transactions) {
          chainLogger.Database_Request();
          using (SafeData sd = Encrypt_Data(data)) {
            apiResponse = new SafeResponse(sd, CodeStatus.Ok);
          }
        }
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("card/transactions/abs")]
  public sealed class Post_CardTransactionsAbs : SafeApi<Request_Transactions> {
    public Post_CardTransactionsAbs(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Get_AbsTransactions([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        using (AutoDisposeList<Abs_Transaction> data = DA.Get_AbsTransactions(ref apiRequest)) {
          chainLogger.Database_Request();
          using (SafeData sd = Encrypt_Data(data)) {
            apiResponse = new SafeResponse(sd, CodeStatus.Ok);
          }
        }
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }
  #endregion card/transactions

  #region money
  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("debet")]
  public sealed class DebetController : SafeApi<SafeApiRequest> {
    public DebetController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_Debet([FromBody] SafeData safeData) {
      try {
        throw new ApiException(CodeStatus.Not_Allowed_Method);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("credit")]
  public sealed class CreditController : SafeApi<SafeApiRequest> {
    public CreditController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_Credit([FromBody] SafeData safeData) {
      try {
        throw new ApiException(CodeStatus.Not_Allowed_Method);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }

  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("virtual_auth")]
  public sealed class VirtualAuthController : SafeApi<SafeApiRequest> {
    public VirtualAuthController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_VirtualAuth([FromBody] SafeData safeData) {
      try {
        throw new ApiException(CodeStatus.Not_Allowed_Method);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }
  #endregion money

  #region Response
  [ApiVersion("2.0")]
  [Produces("application/json")]
  [Route("response")]
  public sealed class ResponseController : SafeApi<Request_Response> {
    public ResponseController(IMemoryCache cache) : base(cache) { }

    [HttpPost]
    public SafeResponse Post_Response([FromBody] SafeData safeData) {
      try {
        Review_HttpRequest(Request);
        Decrypt_Data(ref apiRequest, ref safeData);

        PartnerResponse data = DA.Read_Partner_Response(ref apiRequest);

        SafeData sd = Encrypt_Data(data);

        apiResponse = new SafeResponse(sd, CodeStatus.Ok);
      } catch (ApiException ae) {
        apiResponse.Status = ae.Status;
        Print(ae, Tool.GetCurrentMethod());
      } catch (Exception e) {
        apiResponse.Status = new ResponseStatus(CodeStatus.Unhandled_Exception);
        Print(e, Tool.GetCurrentMethod());
      } finally {
        chainLogger.Finish(apiResponse.Status.Detail);
      }

      return apiResponse;
    }
  }
  #endregion Response
}