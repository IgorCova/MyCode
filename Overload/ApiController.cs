using app.Classes;
using app.overload;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading;

namespace app.Controllers {
  [Route("api")]
  public class ApiController : Controller {
    [HttpGet]
    public string Get() {
      string token = DA.GetToken().GetAwaiter().GetResult();
      return "Started, getting token: " + token;
    }
  }

  [Route("start")]
  public class StartController : Controller {
    [HttpGet]
    public string Get([FromQuery] int cnt, int wait) {
      new Thread(delegate () { DA.Start(cnt, wait * 1000); }).Start();
      return "started";
    }
  }

  [Route("clean")]
  public class CleanController : Controller {
    [HttpGet]
    public string Get() {
      DA.CardsResults.Clear();
      DA.CheckResults.Clear();
      DA.CheckAPIResults.Clear();
      DA.BalanceResults.Clear();
      DA.InfoResults.Clear();
      DA.StatementsResults.Clear();
      return "cleaned";
    }
  }

  [Route("check")]
  public class CheckController : Controller {
    [HttpGet]
    public string Get() {
      IList<TestResult> checks = DA.CheckResults;
      IList<TestResult> cards = DA.CardsResults;
      IList<TestResult> apis = DA.CheckAPIResults;
      IList<TestResult> bal = DA.BalanceResults;
      IList<TestResult> info = DA.InfoResults;
      IList<TestResult> stmt = DA.StatementsResults;

      string checkRes = "Check: " + DA.Check(checks);
      string cardsRes = "Cards: " + DA.Check(cards);
      string apiRes = "Cova API: " + DA.Check(apis);
      string balRes = "Balance: " + DA.Check(bal);
      string infoRes = "Info: " + DA.Check(info);
      string stmtRes = "Statements: " + DA.Check(stmt);

      return String.Format("Test start: {0}, Finish : {1}\n{2}{3}{4}{5}{6}{7}", DA.TestStart.ToUniversalTime().ToString(), DA.TestFinish.ToUniversalTime().ToString(), checkRes, cardsRes, balRes, apiRes, infoRes, stmtRes);
    }
  }
}