using System;
using System.Data;
using System.Collections.Generic;
using Oracle.ManagedDataAccess.Client;
using Newtonsoft.Json;
using Procsender.Schema;
using Proceficator.Schema;

namespace Proceficator {
  internal static partial class DataAccess {
    internal static List<TSelect> Get_Transactions() {
      List<TSelect> info = new List<TSelect>();

      using (OracleConnection connection = new OracleConnection(Environment.Constant.Oracle.oracle_cs)) {
        string query = $"select * from table (proceficator.get_transaction_lifepay)";
        OracleCommand cmd = new OracleCommand(query, connection);
        connection.Open();
        try {
          using (OracleDataReader reader = cmd.ExecuteReader()) {
            while (reader.Read()) {

              Transaction t = new Transaction {
                id = reader.GetIntValue("utrnno"),
                approval = reader.GetStringValue("approval"),
                pan = reader.GetStringValue("pan"),
                created = reader.GetDateTimeOffsetValue("created"),
                amount = reader.GetDecimalValue("amount"),
                rrn = reader.GetStringValue("rrn"),
                payment_id = reader.GetOptionalIntValue("payment_id"),
                mid_rfi = reader.GetStringValue("mid_rfi"),
                tid_vtb = reader.GetStringValue("tid_vtb"),
                cancel_code = reader.GetStringValue("cancel_code"),
                client_id = reader.GetStringValue("client_id"),
                tid_rfi = reader.GetStringValue("tid_rfi"),
                bank_acquirer = reader.GetStringValue("bank_acquirer"),
                command = reader.GetStringValue("command")
              };

              Queue q = new Queue {
                id = reader.GetIntValue("id_queue"),
                utrnno = reader.GetIntValue("utrnno"),
                reversal = reader.GetIntValue("reversal"),
                id_api_user = reader.GetIntValue("id_api_user")
              };

              UserConfig c = new UserConfig {
                method = reader.GetEnumValue<HttpMethod>("method"),
                uri = reader.GetStringValue("uri"),
                headers = reader.GetDictionary<string, string>("headers")
              };

              TSelect ts = new TSelect {
                transaction = t,
                queue = q,
                config = c
              };

              info.Add(ts);
            }
            connection.Close();
            return info;
          }

        } catch (OracleException e) {
          if (e.ErrorCode == -20001) {
            throw new Exception(e.Message);
          } else {
            Console.Out.WriteLine("OracleException: {0}: {1}", Tool.GetCurrentMethod(), e.Message);
            throw new Exception("An error occurred during the operation 'Get_Transactions'");
          }
        }
      }
    }

    internal static void Set_Status_Queue(int id, Queue.Status status) {
      using (OracleConnection connection = new OracleConnection(Environment.Constant.Oracle.oracle_cs)) {
        using (OracleCommand cmd = connection.CreateCommand()) {
          //OracleTransaction transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          //cmd.Transaction = transaction;
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "proceficator.set_status_queue";
          cmd.Parameters.Add(DbParam("in_id", OracleDbType.Int32, id));
          cmd.Parameters.Add(DbParam("in_status", OracleDbType.Int16, status));
          connection.Open();
          try {
            cmd.ExecuteNonQuery();
            //transaction.Commit();
            connection.Close();
          } catch (OracleException e) {
            //transaction.Rollback();
            if (e.ErrorCode == -20001) {
              throw new Exception(e.Message);
            } else {
              Console.Out.WriteLine("OracleException: {0}: {1}", Tool.GetCurrentMethod(), e.Message);
              throw new Exception("An error occurred during the operation 'Set_status_queue'");
            }
          }
        }
      }
    }
  }

  internal static partial class DataAccess {
    private static string GetStringValue(this OracleParameterCollection parameter, string parameterName) {
      string response = string.Empty;
      try {
        response = parameter[parameterName].Value.ToString();
      } catch (Exception e) {
        Console.Out.WriteLine($"{Tool.GetCurrentMethod()} exception: {e.Message}");
      }

      return response;
    }

    private static int GetIntValue(this OracleParameterCollection parameter, string parameterName) {
      bool success = int.TryParse(parameter[parameterName].Value.ToString(), out int response);
      if (success == false) {
        Console.Out.WriteLine($"TryParse parameter: {parameterName}");
        throw new Exception("Fix ME");
      }
      return response;
    }

    private static int GetIntValue(this OracleParameterCollection parameter, string parameterName, out bool success) {
      success = int.TryParse(parameter[parameterName].Value.ToString(), out int response);
      return response;
    }

    private static decimal GetDecimalValue(this OracleParameterCollection parameter, string parameterName) {
      decimal.TryParse(parameter[parameterName].Value.ToString(), out decimal response);
      return response;
    }

    private static OracleParameter DbParam(string name, OracleDbType dbType, object value) {
      OracleParameter prm = new OracleParameter() {
        ParameterName = name,
        OracleDbType = dbType,
        Direction = ParameterDirection.Input,
        Value = value
      };

      return prm;
    }

    private static OracleParameter DbOutParam(string name, OracleDbType dbType) {
      OracleParameter prm = new OracleParameter() {
        ParameterName = name,
        OracleDbType = dbType,
        Direction = ParameterDirection.Output
      };

      return prm;
    }

    private static OracleParameter DbOutParam(string name, OracleDbType dbType, int size) {
      OracleParameter prm = new OracleParameter() {
        ParameterName = name,
        OracleDbType = dbType,
        Size = size,
        Direction = ParameterDirection.Output
      };

      return prm;
    }

    private static OracleParameter DbInOutParam(string name, OracleDbType dbType, int size, object value) {
      OracleParameter prm = new OracleParameter() {
        ParameterName = name,
        OracleDbType = dbType,
        Size = size,
        Direction = ParameterDirection.InputOutput,
        Value = value ?? DBNull.Value
      };

      return prm;
    }

    private static int GetIntValue(this OracleDataReader reader, string parameter) {
      int.TryParse(reader.GetValue(reader.GetOrdinal(name: parameter)).ToString(), out int result);
      return result;
    }

    private static int? GetOptionalIntValue(this OracleDataReader reader, string parameter) {
      if (int.TryParse(reader.GetValue(reader.GetOrdinal(name: parameter)).ToString(), out int result)) 
        return result;

      return null;
    }

    private static decimal GetDecimalValue(this OracleDataReader reader, string parameter) {
      decimal.TryParse(reader.GetValue(reader.GetOrdinal(name: parameter)).ToString(), out decimal result);
      return result;
    }

    private static string GetStringValue(this OracleDataReader reader, string parameter) {
      return reader.GetValue(reader.GetOrdinal(name: parameter)).ToString();
    }

    private static DateTime GetDateTimeValue(this OracleDataReader reader, string parameter) {
      int ordinal = reader.GetOrdinal(name: parameter);
      return reader.GetDateTime(ordinal);
    }

    private static DateTimeOffset GetDateTimeOffsetValue(this OracleDataReader reader, string parameter) {
      int ordinal = reader.GetOrdinal(name: parameter);
      return reader.GetDateTimeOffset(ordinal);
    }

    private static bool GetBooleanValue(this OracleDataReader reader, string parameter) {
      bool.TryParse(reader.GetValue(reader.GetOrdinal(name: parameter)).ToString(), out bool result);
      return result;
    }

    private static TEnum GetEnumValue<TEnum>(this OracleDataReader reader, string parameter) where TEnum : struct {
      Enum.TryParse(reader.GetString(reader.GetOrdinal(parameter)), true, out TEnum result);
      return result;
    }

    private static Dictionary<TKey, TValue> GetDictionary<TKey, TValue>(this OracleDataReader reader, string parameter) {
      return JsonConvert.DeserializeObject<Dictionary<TKey, TValue>>(reader.GetValue(reader.GetOrdinal(name: parameter)).ToString());
    }
  }
}