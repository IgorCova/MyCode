using app.Classes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace app.overload {
  public class DA {
    //private static readonly HttpClient client = new HttpClient();
    public static string token;
    public static IList<TestResult> CheckResults = new List<TestResult>();
    public static IList<TestResult> CheckAPIResults = new List<TestResult>();
    public static IList<TestResult> CardsResults = new List<TestResult>();
    public static IList<TestResult> BalanceResults = new List<TestResult>();
    public static IList<TestResult> InfoResults = new List<TestResult>();
    public static IList<TestResult> StatementsResults = new List<TestResult>();
    public static DateTime TestStart;
    public static DateTime TestFinish;

    internal static string Check(IList<TestResult> Results) {
      string count = Results.Count.ToString();
      int success = 0;
      foreach (TestResult res in Results) {
        if (res.Status == true) {
          success++;
        }
      }

      return String.Format("request count {0}, success: {1}\n", count.ToString(), success);
    }

    internal static void Start(int cycles, int wait) {
      TestStart = DateTime.Now;
      for (int i = 0; i < 60; i++) {
      //  new Thread(delegate () { Test_CheckAPI(cycles); }).Start();
       // new Thread(delegate () { Test_Check(cycles); }).Start();
        new Thread(delegate () { Test_Cards(cycles); }).Start();
        new Thread(delegate () { Test_Balance(cycles); }).Start();
        new Thread(delegate () { Test_Info(cycles); }).Start();
        new Thread(delegate () { Test_Statements(cycles); }).Start();
        Thread.Sleep(wait);
      }
    }

    internal static void Test_CheckAPI(int cls) {
      for (int cycles = 0; cycles < cls; cycles++) {
        Task task = CheckAPI(cycles);
      }
    }

    internal static void Test_Check(int cls) {
      for (int cycles = 0; cycles < cls; cycles++) {
        Task task = Check(cycles);
      }
    }

    internal static void Test_Cards(int cls) {
      for (int cycles = 0; cycles < cls; cycles++) {
        Task task = Cards(cycles);
      }
    }

    internal static void Test_Balance(int cls) {
      for (int cycles = 0; cycles < cls; cycles++) {
        Task task = Balance(cycles);
      }
    }

    internal static void Test_Info(int cls) {
      for (int cycles = 0; cycles < cls; cycles++) {
        Task task = Info(cycles);
      }
    }

    internal static void Test_Statements(int cls) {
      for (int cycles = 0; cycles < cls; cycles++) {
        Task task = Statements(cycles);
      }
    }

    internal static async Task CheckAPI(int i) {
      Console.Out.WriteLine("Start Check " + i.ToString() + " " + DateTime.Now.ToUniversalTime());
      try {
        using (HttpClient http = new HttpClient())
        using (HttpResponseMessage response = await http.GetAsync("https://cova.company/check")) {
          string responseString = await response.Content.ReadAsStringAsync();

          if (response.IsSuccessStatusCode) {
            CheckAPIResults.Add(item: new TestResult((responseString == "Success")));
          } else {
            Console.Out.WriteLine("Check Failed");
            Console.Out.WriteLine(responseString);
            CheckAPIResults.Add(item: new TestResult(false));
          }
        }
      } catch (Exception er) {
        Console.Out.WriteLine("Error: Check / {0}", er.Message);
      }

      Console.Out.WriteLine("Finich Check " + i.ToString() + " " + DateTime.Now.ToUniversalTime());
      TestFinish = DateTime.Now;
    }

    internal static async Task Check(int i) {
      Console.Out.WriteLine("Start Check " + i.ToString() + " " + DateTime.Now.ToShortTimeString());
      try {
        using (HttpClient http = new HttpClient()) {
          http.DefaultRequestHeaders.Add("Authorization", token);
          using (HttpResponseMessage response = await http.GetAsync("http://pc-api.preprod.bank.rfi/check")) {
            if (response.IsSuccessStatusCode) {
              string responseString = await response.Content.ReadAsStringAsync();
              CheckResults.Add(item: new TestResult(true));
            } else {
              Console.Out.WriteLine("Check Failed");
              CheckResults.Add(item: new TestResult(false));
            }
          }
        }
      } catch (Exception er) {
        Console.Out.WriteLine("Error: Check / {0}", er.Message);
      }

      Console.Out.WriteLine("Finich Check " + i.ToString() + " " + DateTime.Now.ToShortTimeString());
      TestFinish = DateTime.Now;
    }

    internal static async Task Cards(int i) {
      Console.Out.WriteLine("Start Cards " + i.ToString() + " " + DateTime.Now.ToShortTimeString());
      try {
        using (HttpClient http = new HttpClient()) {
          http.DefaultRequestHeaders.Add("Authorization", token);
          using (HttpResponseMessage response = await http.GetAsync("http://pc-api.preprod.bank.rfi/cards")) {
            string json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode) {
              CardListResponse cards = JsonConvert.DeserializeObject<CardListResponse>(json);
              CardsResults.Add(item: new TestResult(cards.Status == "Success"));
            } else {
              CardsResults.Add(item: new TestResult(false));
            }
          }
        }
      } catch (Exception er) {
        Console.Out.WriteLine("Error: Cards / {0}", er.Message);
        CardsResults.Add(item: new TestResult(false));
      }
      Console.Out.WriteLine("Finich Cards " + i.ToString() + DateTime.Now.ToShortTimeString());
      TestFinish = DateTime.Now;
    }

    internal static async Task Balance(int i) {
      Console.Out.WriteLine("Start Balance " + i.ToString() + " " + DateTime.Now.ToShortTimeString());
      try {
        using (HttpClient http = new HttpClient()) {
          http.DefaultRequestHeaders.Add("Authorization", token);
          using (HttpResponseMessage response = await http.GetAsync("http://pc-api.preprod.bank.rfi/cards/82/balance")) {
            string json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode) {
              CardBalanceResponse balance = JsonConvert.DeserializeObject<CardBalanceResponse>(json);
              BalanceResults.Add(item: new TestResult(balance.Status == "Success"));
            } else {
              BalanceResults.Add(item: new TestResult(false));
            }
          }
        }
      } catch (Exception er) {
        Console.Out.WriteLine("Error: Balance / {0}", er.Message);
        BalanceResults.Add(item: new TestResult(false));
      }
      Console.Out.WriteLine("Finich Balance " + i.ToString() + DateTime.Now.ToShortTimeString());
      TestFinish = DateTime.Now;
    }

    internal static async Task Info(int i) {
      Console.Out.WriteLine("Start Info " + i.ToString() + " " + DateTime.Now.ToShortTimeString());
      try {
        using (HttpClient http = new HttpClient()) {
          http.DefaultRequestHeaders.Add("Authorization", token);
          using (HttpResponseMessage response = await http.GetAsync("http://pc-api.preprod.bank.rfi/cards/82/info")) {
            string json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode) {
              CurrentCardBalanceResponse inf = JsonConvert.DeserializeObject<CurrentCardBalanceResponse>(json);
              InfoResults.Add(item: new TestResult(inf.Status == "Success"));
            } else {
              InfoResults.Add(item: new TestResult(false));
            }
          }
        }
      } catch (Exception er) {
        Console.Out.WriteLine("Error: Info / {0}", er.Message);
        InfoResults.Add(item: new TestResult(false));
      }
      Console.Out.WriteLine("Finich Info " + i.ToString() + DateTime.Now.ToShortTimeString());
      TestFinish = DateTime.Now;
    }


    internal static async Task Statements(int i) {
      Console.Out.WriteLine("Start Statements " + i.ToString() + " " + DateTime.Now.ToShortTimeString());
      try {
        using (HttpClient http = new HttpClient()) {
          http.DefaultRequestHeaders.Add("Authorization", token);
          using (HttpResponseMessage response = await http.GetAsync("http://pc-api.preprod.bank.rfi/cards/82/statements?date_from=2018-06-01T00:00:00&date_to=2018-06-21T00:00:00")) {
            string json = await response.Content.ReadAsStringAsync();
            if (response.IsSuccessStatusCode) {
              StatementListResponse stmt = JsonConvert.DeserializeObject<StatementListResponse>(json);
              StatementsResults.Add(item: new TestResult(stmt.Status == "Success"));
            } else {
              StatementsResults.Add(item: new TestResult(false));
            }
          }
        }
      } catch (Exception er) {
        Console.Out.WriteLine("Error: Info / {0}", er.Message);
        StatementsResults.Add(item: new TestResult(false));
      }
      Console.Out.WriteLine("Finich Statements " + i.ToString() + DateTime.Now.ToShortTimeString());
      TestFinish = DateTime.Now;
    }

    internal static async Task<string> GetToken() {
      Dictionary<string, string> values = new Dictionary<string, string> {
        { "username", "foodvam_test" }, {"password", "sAuTqQ7hA3QfMwqf"}
      };

      try {
        FormUrlEncodedContent content = new FormUrlEncodedContent(values);
        using (HttpClient http = new HttpClient())
        using (HttpResponseMessage response = await http.PostAsync("https://sso.rfibank.ru/api/token-auth/", content)) {

          string responseString = await response.Content.ReadAsStringAsync();

          if (response.IsSuccessStatusCode) {
            TokenResp result = JsonConvert.DeserializeObject<TokenResp>(responseString);
            token = result.Token;
            if (String.IsNullOrEmpty(token) == true)
              throw new ApiException("Token is invalid");
          }
        }
      } catch (Exception er) {
        Console.Out.WriteLine("Error: SSO get token / {0}", er.Message);
      }

      //      client.DefaultRequestHeaders.Add("Authorization", token);

      return token;
    }

  }
}