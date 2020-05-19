using Api.Classes;
using Api.Interfaces;
using Api.Tools;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;

namespace Api {
  internal static partial class DataAccess {
    #region Access
    internal static bool Access_Authorization(int idUser, int idPartner, int idCardType, int idCard) {
      bool result = false;

      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.access_authorization";

          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcardtype", OracleDbType.Int32, idCardType));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, idCard));

          cmd.Parameters.Add(DbOutParam("out_success", OracleDbType.Int32, 1));

          result = cmd.ExecuteProcedure();

          if (result) {
            int out_success = cmd.Parameters.GetIntValue("out_success");
            result = (out_success == 1);
          }

          connection.Close();
        }
      }

      return result;
    }
    #endregion Access

    #region Cards
    internal static AutoDisposeList<Card> Get_Cards(ref Request_Cards request) {
      return Get_Cards(request.User_ID, request.Partner_ID, request.Id_Card, request.Ids_Cards);
    }

    internal static AutoDisposeList<Card> Get_Cards(int idUser, int idPartner, int idCard = 0, string idsCards = "0") {
      using (AutoDisposeList<Card> response = new AutoDisposeList<Card>())
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.Text;
          cmd.BindByName = true;

          string commandText = $"select * from table (API.get_cards(in_iduser => :in_iduser, in_idpartner => :in_idpartner";

          if ((idCard != 0) || (idsCards != "0")) {
            commandText += ", in_idcard => :in_idcard, in_ids_cards => :in_ids_cards";
          }

          commandText += "))";

          cmd.CommandText = commandText;

          OracleParameter in_iduser = new OracleParameter("in_iduser", idUser);
          cmd.Parameters.Add(in_iduser);

          OracleParameter in_idpartner = new OracleParameter("in_idpartner", idPartner);
          cmd.Parameters.Add(in_idpartner);

          if ((idCard != 0) || (idsCards != "0")) {
            OracleParameter in_idcard = new OracleParameter("in_idcard", idCard);
            cmd.Parameters.Add(in_idcard);

            OracleParameter in_ids_cards = new OracleParameter("in_ids_cards", idsCards);
            cmd.Parameters.Add(in_ids_cards);
          }

          try {
            OracleDataReader reader = cmd.ExecuteReader();

            if (reader.HasRows == false) {
              throw new ApiException(CodeStatus.Data_Not_Found);
            }

            while (reader.Read()) {
              response.Add(new Card() {
                ID_Card = reader.GetIntValue("id_card"),
                Exp_Date = reader.GetStringValue("exp_date"),
                ID_Status = reader.GetIntValue("id_status"),
                Masked_PAN = reader.GetStringValue("masked_pan"),
                Name = reader.GetStringValue("name"),
                Embosing_Name = reader.GetStringValue("embosing_name"),
                Status_Name = reader.GetStringValue("status_name"),
                ID_Key = reader.GetStringValue("id_key"),
                Card_Type_ID = reader.GetIntValue("card_type_id"),
                Sequence_PAN = reader.GetIntValue("sequence_pan"),
                Card_Account = reader.GetStringValue("card_account"),
                Vid = reader.GetStringValue("vid")
              });
            }

            reader.Dispose();
          } catch (OracleException oe) {
            oe.ThrowFrom(cmd.CommandText, Tool.GetCurrentMethod());
          }
          connection.Close();
        }
        return response;
      }
    }

    internal static CardInfo Get_Card_Info(ref Request_Card request) {
      using (CardInfo response = new CardInfo())
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.get_card_info";

          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, request.User_ID));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, request.Partner_ID));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, request.Id_Card));

          cmd.Parameters.Add(DbOutParam("out_name", OracleDbType.Varchar2, 200));
          cmd.Parameters.Add(DbOutParam("out_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_amount_real", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_hold_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_currency", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_id_status", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_status_name", OracleDbType.Varchar2, 100));
          cmd.Parameters.Add(DbOutParam("out_sms_informing", OracleDbType.Varchar2, 3));
          cmd.Parameters.Add(DbOutParam("out_email_informing", OracleDbType.Varchar2, 3));

          cmd.ExecuteProcedure();

          response.Name = cmd.Parameters.GetStringValue("out_name");
          response.Amount = cmd.Parameters.GetDecimalValue("out_amount");
          response.Amount_Real = cmd.Parameters.GetDecimalValue("out_amount_real");
          response.Holds = cmd.Parameters.GetDecimalValue("out_hold_amount");
          response.Currency = cmd.Parameters.GetIntValue("out_currency");
          response.ID_Status = cmd.Parameters.GetIntValue("out_id_status");
          response.Status_Name = cmd.Parameters.GetStringValue("out_status_name");
          response.Sms_Informing = cmd.Parameters.GetStringValue("out_sms_informing");
          response.Email_Informing = cmd.Parameters.GetStringValue("out_email_informing");

          if (string.IsNullOrEmpty(response.Name)) {
            throw new ApiException(CodeStatus.Unhandled_Exception, $"Get_Card_Info ABS doesn't answer: {request.User_ID}, {request.Partner_ID}, {request.Id_Card}");
          }
        }

        return response;
      }
    }

    internal static CardInfo Get_Card_Info_Joint(ref Request_Card request) {
      return Get_Card_Info_Joint(request.User_ID, request.Partner_ID, request.Id_Card);
    }

    internal static CardInfo Get_Card_Info_Joint(int idUser, int idPartner, int idCard) {
      using (CardInfo response = new CardInfo())
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.get_card_info_joint";

          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, idCard));

          cmd.Parameters.Add(DbOutParam("out_name", OracleDbType.Varchar2, 200));
          cmd.Parameters.Add(DbOutParam("out_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_aval_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_rur_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_curr_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_real_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_hold_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_currency", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_id_status", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_status_name", OracleDbType.Varchar2, 100));
          cmd.Parameters.Add(DbOutParam("out_sms_informing", OracleDbType.Varchar2, 3));
          cmd.Parameters.Add(DbOutParam("out_email_informing", OracleDbType.Varchar2, 3));

          cmd.ExecuteProcedure();

          response.Name = cmd.Parameters.GetStringValue("out_name");
          response.Amount = cmd.Parameters.GetDecimalValue("out_amount");
          response.Amount_Real = cmd.Parameters.GetDecimalValue("out_real_amount");
          response.Holds = cmd.Parameters.GetDecimalValue("out_hold_amount");
          response.Currency = cmd.Parameters.GetIntValue("out_currency");
          response.ID_Status = cmd.Parameters.GetIntValue("out_id_status");
          response.Status_Name = cmd.Parameters.GetStringValue("out_status_name");
          response.Sms_Informing = cmd.Parameters.GetStringValue("out_sms_informing");
          response.Email_Informing = cmd.Parameters.GetStringValue("out_email_informing");

          if (string.IsNullOrEmpty(response.Name)) {
            throw new ApiException(CodeStatus.Unhandled_Exception, $"Get_Card_Info_Joint ABS doesn't answer: {idUser}, {idPartner}, {idCard}");
          }
        }

        return response;
      }
    }

    internal static CardBalance Get_CardBalance(int idUser, int idPartner, int idCard) {
      using (CardBalance response = new CardBalance())
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.get_card_info_joint";

          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, idCard));

          cmd.Parameters.Add(DbOutParam("out_name", OracleDbType.Varchar2, 200));
          cmd.Parameters.Add(DbOutParam("out_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_aval_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_rur_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_curr_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_real_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_hold_amount", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_currency", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_id_status", OracleDbType.Int32, 14));
          cmd.Parameters.Add(DbOutParam("out_status_name", OracleDbType.Varchar2, 100));
          cmd.Parameters.Add(DbOutParam("out_sms_informing", OracleDbType.Varchar2, 3));
          cmd.Parameters.Add(DbOutParam("out_email_informing", OracleDbType.Varchar2, 3));
          cmd.ExecuteProcedure();

          response.Name = cmd.Parameters.GetStringValue("out_name");
          response.Amount = cmd.Parameters.GetDecimalValue("out_amount");
          response.Amount_All = cmd.Parameters.GetDecimalValue("out_aval_amount");
          response.Amount_Real = cmd.Parameters.GetDecimalValue("out_real_amount");

          if (string.IsNullOrEmpty(response.Name)) {
            Console.Out.WriteLine($"Get_Card_Info ABS doesn't answer: {idUser}, {idPartner}, {idCard}");
            throw new ApiException(CodeStatus.Unhandled_Exception);
          }
        }

        connection.Close();

        return response;
      }
    }

    internal static VirtualCardOut Create_VirtualCard(ref Request_VirtualCard request, ref VirtualCard card) {
      return Create_VirtualCard(request.User_ID, request.Partner_ID, card);
    }

    internal static VirtualCardOut Create_VirtualCard(int IdUser, int idPartner, VirtualCard card) {
      using (VirtualCardOut response = new VirtualCardOut())
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.create_virtual_card";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, IdUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcardtype", OracleDbType.Int32, card.Card_Type_ID));
          cmd.Parameters.Add(DbParam("in_exp_date", OracleDbType.Varchar2, card.Exp_Date));
          cmd.Parameters.Add(DbParam("in_id_status", OracleDbType.Int32, card.ID_Status));
          cmd.Parameters.Add(DbParam("in_name", OracleDbType.Varchar2, card.Name));
          cmd.Parameters.Add(DbParam("in_phone_Int32", OracleDbType.Varchar2, card.Phone_No));
          cmd.Parameters.Add(DbParam("in_sms_auth", OracleDbType.Varchar2, card.Informing.ToLower() == "on" ? "YES" : "NO"));
          cmd.Parameters.Add(DbParam("in_currency", OracleDbType.Int32, card.Currency));
          cmd.Parameters.Add(DbParam("in_code_word", OracleDbType.Varchar2, card.Code_Word));

          cmd.Parameters.Add(DbOutParam("out_id_card", OracleDbType.Int32, 12));
          cmd.Parameters.Add(DbOutParam("out_id_key", OracleDbType.Varchar2, 11));
          cmd.Parameters.Add(DbOutParam("out_masked_pan", OracleDbType.Varchar2, 25));
          cmd.Parameters.Add(DbOutParam("out_cvc2", OracleDbType.Varchar2, 3));
          cmd.Parameters.Add(DbOutParam("out_remaining_limit", OracleDbType.Int32, 6));
          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          cmd.ExecuteProcedureWithCheckOut();

          response.ID_Card = cmd.Parameters.GetIntValue("out_id_card");
          response.ID_Key = cmd.Parameters.GetStringValue("out_id_key");
          response.Masked_PAN = cmd.Parameters.GetStringValue("out_masked_pan");
          response.Remaining_limit = cmd.Parameters.GetIntValue("out_remaining_limit");

          connection.Close();
        }

        return response;
      }
    }

    internal static CardCvc2 Get_CardCVC2(ref Request_Card request) {
      using (CardCvc2 response = new CardCvc2())
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.get_card_cvc2";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, request.User_ID));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, request.Partner_ID));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, request.Id_Card));

          cmd.Parameters.Add(DbOutParam("out_cvc2", OracleDbType.Int32, 3));
          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          cmd.ExecuteProcedureWithCheckOut();

          response.Cvc2 = cmd.Parameters.GetStringValue("out_cvc2");
          connection.Close();
        }

        return response;
      }
    }

    internal static bool CardDetail_Inform(ref Request_CardDetail request) {
      bool response = false;
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {

        connection.Open();

        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.card_detail_inform";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, request.User_ID));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, request.Partner_ID));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, request.Id_Card));
          cmd.Parameters.Add(DbParam("in_info_type", OracleDbType.Int32, request.Info_Type));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          response = cmd.ExecuteProcedureWithCheckOut();

          connection.Close();
        }
      }

      return response;
    }
    #endregion Cards

    #region CardLimit
    internal static bool Set_CardLimit(ref Request_SetLimit request) {
      return Set_CardLimit(request.User_ID, request.Partner_ID, request.Id_Card, request.Amount);
    }

    internal static bool Set_CardLimit(int idUser, int idPartner, int idCard, int limit) {
      bool response = false;
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {

        connection.Open();

        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.set_limit";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, idCard));
          cmd.Parameters.Add(DbParam("in_limit", OracleDbType.Int32, limit));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          response = cmd.ExecuteProcedureWithCheckOut();

          connection.Close();
        }
      }

      return response;
    }

    internal static bool Clear_CardLimit(ref Request_ClearLimit request) {
      return Clear_CardLimit(request.User_ID, request.Partner_ID, request.Id_Card);
    }

    internal static bool Clear_CardLimit(int idUser, int idPartner, int idCard) {
      bool response = false;
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.clear_limit";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, idCard));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          response = cmd.ExecuteProcedureWithCheckOut();

          connection.Close();
        }

        return response;
      }
    }
    #endregion CardLimit

    #region Filter
    internal static AutoDisposeList<FilterMerch> Get_Filters(int idUser, int idPartner, int idCardType) {
      using (AutoDisposeList<FilterMerch> response = new AutoDisposeList<FilterMerch>())
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();

        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.Text;
          cmd.CommandText = $"select * from table(API.get_filters(" +
            $"in_iduser => {idUser}," +
            $"in_idpartner => {idPartner}, " +
            $"in_idcardtype => {idCardType}))";

          try {
            OracleDataReader er = cmd.ExecuteReader();

            if (er.HasRows == false) {
              throw new ApiException(CodeStatus.Data_Not_Found);
            }

            while (er.Read()) {
              response.Add(new FilterMerch() {
                ID = er.GetIntValue("id"),
                Mask = er.GetStringValue("mask")
              });
            }
          } catch (OracleException oe) {
            oe.ThrowFrom(cmd.CommandText, Tool.GetCurrentMethod());
          }
          connection.Close();
        }
        return response;
      }
    }

    internal static Response Save_Filter(int idUser, int idPartner, int idCardType, string mask) {
      if (mask == null) {
        throw new ArgumentNullException(nameof(mask));
      }

      Response response = new Response();
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {

        connection.Open();

        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.save_filter_merch";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcardtype", OracleDbType.Int32, idCardType));
          cmd.Parameters.Add(DbParam("in_mask", OracleDbType.Varchar2, mask));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          response.Status = cmd.ExecuteProcedureWithCheckOut() == true ? "Success" : "Fail";
          connection.Close();
        }
      }

      return response;
    }

    internal static Response Delete_Filter(int idUser, int idPartner, int id) {
      Response response = new Response();
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.delete_filter_merch";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_id", OracleDbType.Int32, id));
          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          response.Status = cmd.ExecuteProcedureWithCheckOut() == true ? "Success" : "Fail";
          connection.Close();
        }
      }

      return response;
    }
    #endregion Filter

    #region Transactions
    internal static AutoDisposeList<Transaction> Get_CardStatements(int idUser, int idPartner, int idCard, Dates dates) {
      using (AutoDisposeList<Transaction> response = new AutoDisposeList<Transaction>())
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();

        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.Text;
          cmd.CommandText = $"select * from table(API.get_trans_by_card(" +
            $"in_iduser => {idUser}, " +
            $"in_idpartner => {idPartner}, " +
            $"in_idcard => {idCard}, " +
            $"in_datefrom => to_date('{dates.Date_From.ToString("dd/MM/yyyy HH:mm:ss")}', 'dd/mm/yyyy hh24:mi:ss'), " +
            $"in_dateto => to_date('{dates.Date_To.ToString("dd/MM/yyyy HH:mm:ss")}', 'dd/mm/yyyy hh24:mi:ss')))";

          try {
            OracleDataReader er = cmd.ExecuteReader();

            if (er.HasRows == false) {
              throw new ApiException(CodeStatus.Data_Not_Found);
            }

            while (er.Read()) {
              response.Add(new Transaction() {
                ID_Trans = er.GetIntValue("id_trans"),
                Auth_ID_Resp = er.GetStringValue("authidresp"),
                Date = er.GetDateTimeValue("dt"),
                Merchant = er.GetStringValue("merchant"),
                Currency = er.GetStringValue("currency"),
                Req_Amount = er.GetDecimalValue("reqamt"),
                Con_Currency = er.GetStringValue("concurrency"),
                Con_Amount = er.GetDecimalValue("conamt"),
                Fee_Amount = er.GetDecimalValue("feeamt"),
                ID_Status = er.GetIntValue("status"),
                Status_Name = er.GetStringValue("status_name"),
                Is_Reversal = er.GetBooleanValue("is_reversal"),
                Has_Reversal = er.GetBooleanValue("has_reversal"),
                Trans_Type_Name = er.GetStringValue("trans_type_name"),
                Mcc = er.GetIntValue("mcc"),
                Country = er.GetStringValue("country"),
                City = er.GetStringValue("city")
              });
            }
          } catch (OracleException oe) {
            oe.ThrowFrom(cmd.CommandText, Tool.GetCurrentMethod());
          }
          connection.Close();
        }
        return response;
      }
    }

    internal static TransactionPage Get_Transactions(ref Request_TopTransactions request) {
      return Get_Transactions(request.User_ID, request.Partner_ID, request.Id_Card, new Dates(), 0, request.Top);
    }

    internal static TransactionPage Get_Transactions(ref Request_Transactions request) {
      return Get_Transactions(request.User_ID, request.Partner_ID, request.Id_Card, new Dates(request.Date_From, request.Date_To, true), request.Page);
    }

    private static TransactionPage Get_Transactions(int idUser, int idPartner, int idCard, Dates dates, int page, int top = 100) {
      using (TransactionPage response = new TransactionPage())
      using (response.Transactions = new AutoDisposeList<Transaction>())
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();

        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.Text;
          cmd.CommandText = $"select * from table(API.get_trans_by_card(" +
            $"in_iduser => {idUser}, " +
            $"in_idpartner => {idPartner}, " +
            $"in_idcard => {idCard}, " +
            $"in_datefrom => to_date('{dates.Date_From.ToString("dd/MM/yyyy HH:mm:ss")}', 'dd/mm/yyyy hh24:mi:ss'), " +
            $"in_dateto => to_date('{dates.Date_To.ToString("dd/MM/yyyy HH:mm:ss")}', 'dd/mm/yyyy hh24:mi:ss'), " +
            $"in_page => {page}, " +
            $"in_top => {top}))";

          try {
            OracleDataReader er = cmd.ExecuteReader();

            if (er.HasRows == false) {
              throw new ApiException(CodeStatus.Data_Not_Found);
            }

            while (er.Read()) {
              response.Transactions.Add(new Transaction() {
                ID_Trans = er.GetIntValue("id_trans"),
                Auth_ID_Resp = er.GetStringValue("authidresp"),
                Date = er.GetDateTimeValue("dt"),
                Merchant = er.GetStringValue("merchant"),
                Currency = er.GetStringValue("currency"),
                Req_Amount = er.GetDecimalValue("reqamt"),
                Con_Currency = er.GetStringValue("concurrency"),
                Con_Amount = er.GetDecimalValue("conamt"),
                Fee_Amount = er.GetDecimalValue("feeamt"),
                ID_Status = er.GetIntValue("status"),
                Status_Name = er.GetStringValue("status_name"),
                Is_Reversal = er.GetBooleanValue("is_reversal"),
                Has_Reversal = er.GetBooleanValue("has_reversal"),
                Trans_Type_Name = er.GetStringValue("trans_type_name"),
                Mcc = er.GetIntValue("mcc"),
                Country = er.GetStringValue("country"),
                City = er.GetStringValue("city")
              });

              response.Rows_Total = er.GetIntValue("rows_total");
            }
          } catch (OracleException oe) {
            oe.ThrowFrom(cmd.CommandText, Tool.GetCurrentMethod());
          }
          connection.Close();
        }
        return response;
      }
    }

    internal static AutoDisposeList<Abs_Transaction> Get_AbsTransactions(ref Request_Transactions request) {
      using (AutoDisposeList<Abs_Transaction> response = new AutoDisposeList<Abs_Transaction>())
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        Dates dates = new Dates(request.Date_From, request.Date_To, true);
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.Text;
          cmd.CommandText = $"select * from table(API.get_trans_by_card_abs (" +
            $"in_iduser => {request.User_ID}, " +
            $"in_idpartner => {request.Partner_ID}, " +
            $"in_idcard => {request.Id_Card}, " +
            $"in_datefrom => to_date('{dates.Date_From.ToString("dd/MM/yyyy HH:mm:ss")}', 'dd/mm/yyyy hh24:mi:ss'), " +
            $"in_dateto => to_date('{dates.Date_To.ToString("dd/MM/yyyy HH:mm:ss")}', 'dd/mm/yyyy hh24:mi:ss')))";

          try {
            OracleDataReader er = cmd.ExecuteReader();

            if (er.HasRows == false) {
              throw new ApiException(CodeStatus.Data_Not_Found);
            }

            while (er.Read()) {
              response.Add(new Abs_Transaction() {
                Auth_Code = er.GetStringValue("auth_code"),
                Date = er.GetDateTimeValue("dt_tran_card"),
                Merchant = er.GetStringValue("merchant"),
                Currency = er.GetStringValue("tran_currency"),
                Req_Amount = er.GetDecimalValue("tran_amt"),
                Fee_Amount = er.GetDecimalValue("fee_amt"),
                Is_Reversal = er.GetIntValue("is_reversal"),
                Has_Reversal = er.GetStringValue("has_reversal"),
                Mcc = er.GetIntValue("mcc"),
                Device = er.GetStringValue("device"),
                Acc_Amount = er.GetDecimalValue("acc_amt"),
                Acc_Currency = er.GetStringValue("acc_cur"),
                RRN = er.GetStringValue("rrn")
              });
            }
          } catch (OracleException oe) {
            oe.ThrowFrom(cmd.CommandText, Tool.GetCurrentMethod());
          }
          connection.Close();
        }

        return response;
      }
    }
    #endregion Transactions

    #region Informing

    internal static bool Set_Phone(ref Request_SetPhone request) {
      return Set_Phone(request.User_ID, request.Partner_ID, request.Id_Card, request.Phone_No);
    }

    internal static bool Set_Phone(int idUser, int idPartner, int idCard, string phone) {
      bool response = false;

      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.sms_set_phone";

          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, idCard));
          cmd.Parameters.Add(DbParam("in_phone", OracleDbType.Varchar2, phone));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          response = cmd.ExecuteProcedureWithCheckOut();
          connection.Close();
        }
      }

      return response;
    }

    internal static bool Sms_Informing(ref Request_CardSms request) {
      return Sms_Informing(request.User_ID, request.Partner_ID, request.Id_Card, request.Status);
    }

    internal static bool Sms_Informing(int idUser, int idPartner, int idCard, string state) {
      bool response = false;
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();

        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.sms_informing";

          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, idCard));
          cmd.Parameters.Add(DbParam("in_state", OracleDbType.Varchar2, state));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          response = cmd.ExecuteProcedureWithCheckOut();
          connection.Close();
        }
      }

      return response;
    }
    #endregion Informing

    #region Card block
    internal static bool Block_Card(ref Request_BlockCard request) {
      return Block_Card(request.User_ID, request.Partner_ID, request.Id_Card, request.Reason);
    }

    internal static bool Block_Card(int idUser, int idPartner, int idCard, int reason) {
      bool response = false;
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.block_card";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, idCard));
          cmd.Parameters.Add(DbParam("in_block_reason", OracleDbType.Varchar2, reason));
          cmd.Parameters.Add(DbParam("in_block_reasontxt", OracleDbType.Varchar2, "Blocked by request via PC Open API"));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          response = cmd.ExecuteProcedureWithCheckOut();
          connection.Close();
        }
      }

      return response;
    }

    internal static bool UnBlock_Card(ref Request_BlockCard request) {
      return UnBlock_Card(request.User_ID, request.Partner_ID, request.Id_Card);
    }

    internal static bool UnBlock_Card(int idUser, int idPartner, int idCard) {
      bool response = false;
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.unblock_card";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, idCard));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          response = cmd.ExecuteProcedureWithCheckOut();
          connection.Close();
        }
      }

      return response;
    }
    #endregion Card block

    #region Work with PIN
    internal static bool Set_Pin(ref Request_SetPin request) {
      Tool.Validate_Pin(request.Pin, out int pin_code);
      return Set_Pin(request.User_ID, request.Partner_ID, request.Id_Card, pin_code);
    }

    internal static bool Set_Pin(int idUser, int idPartner, int idCard, int pin) {
      bool response = false;
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.change_pin";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, idUser));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, idPartner));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, idCard));
          cmd.Parameters.Add(DbParam("in_pin", OracleDbType.Int32, pin));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          response = cmd.ExecuteProcedureWithCheckOut();
          connection.Close();
        }
      }

      return response;
    }

    internal static bool Reset_Pin_Counter(ref Request_Card request) {
      bool response = false;
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.Transaction = connection.BeginTransaction(IsolationLevel.ReadCommitted);
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.reset_pin_counter";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, request.User_ID));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, request.Partner_ID));
          cmd.Parameters.Add(DbParam("in_idcard", OracleDbType.Int32, request.Id_Card));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          response = cmd.ExecuteProcedureWithCheckOut();

          connection.Close();
        }
      }

      return response;
    }
    #endregion Work with PIN

    #region Partner response
    internal static void Response_Save<R>(R request, string response, ResponseStatus status) where R : IRequest {
      if (status.Code == (int)CodeStatus.Duplicate_Request) {
        return;
      }

      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.api_response_save";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, request.User_ID));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, request.Partner_ID));
          cmd.Parameters.Add(DbParam("in_idrequest", OracleDbType.Int32, request.Request_ID));
          cmd.Parameters.Add(DbParam("in_response", OracleDbType.Varchar2, response));
          cmd.Parameters.Add(DbParam("in_method", OracleDbType.Varchar2, request.Path));
          cmd.Parameters.Add(DbParam("in_type", OracleDbType.Varchar2, request.Method));
          cmd.Parameters.Add(DbParam("in_version", OracleDbType.Varchar2, request.Version));
          cmd.Parameters.Add(DbParam("in_status", OracleDbType.Varchar2, status.ToJson()));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 256));

          cmd.ExecuteProcedureWithCheckOut();
          connection.Close();
        }
      }
    }

    internal static PartnerResponse Read_Partner_Response(ref Request_Response request) {
      PartnerResponse response = new PartnerResponse();

      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.api_response_read";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, request.User_ID));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, request.Partner_ID));
          cmd.Parameters.Add(DbParam("in_idrequest", OracleDbType.Int32, request.RequiredRequest_ID));
          cmd.Parameters.Add(DbParam("in_method", OracleDbType.Varchar2, request.RequiredMethod));

          cmd.Parameters.Add(DbOutParam("out_response", OracleDbType.Varchar2, 4000));
          cmd.Parameters.Add(DbOutParam("out_status", OracleDbType.Varchar2, 4000));
          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 4000));

          cmd.ExecuteProcedureWithCheckOut();

          response.Data = cmd.Parameters.GetStringValue("out_response");
          response.Status = JsonConvert.DeserializeObject<ResponseStatus>(cmd.Parameters.GetStringValue("out_status"));

          connection.Close();
        }
      }

      return response;
    }
    #endregion Partner response
    internal static void Check() {
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "sp_check_dotnet_core";

          cmd.ExecuteProcedure();

          connection.Close();
        }
      }
    }

    internal static void Request_Save(IRequest request) {
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.api_request_save";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, request.User_ID));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, request.Partner_ID));
          cmd.Parameters.Add(DbParam("in_idrequest", OracleDbType.Int32, request.Request_ID));
          cmd.Parameters.Add(DbParam("in_method", OracleDbType.Varchar2, request.Path));
          cmd.Parameters.Add(DbParam("in_type", OracleDbType.Varchar2, request.Method));
          cmd.Parameters.Add(DbParam("in_version", OracleDbType.Decimal, request.Version));
          cmd.Parameters.Add(DbParam("in_dateRequest", OracleDbType.Date, request.Date));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 4000));

          cmd.ExecuteProcedureWithCheckOut();
          connection.Close();
        }
      }
    }

    internal static void Method_Authorization(IRequestBase request) {
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.StoredProcedure;
          cmd.CommandText = "API.api_method_authorization";
          cmd.Parameters.Add(DbParam("in_iduser", OracleDbType.Int32, request.User_ID));
          cmd.Parameters.Add(DbParam("in_idpartner", OracleDbType.Int32, request.Partner_ID));
          cmd.Parameters.Add(DbParam("in_method", OracleDbType.Varchar2, request.Path));
          cmd.Parameters.Add(DbParam("in_type", OracleDbType.Varchar2, request.Method));
          cmd.Parameters.Add(DbParam("in_version", OracleDbType.Decimal, request.Version));

          cmd.Parameters.Add(DbOutParam("out_errnum", OracleDbType.Int32, 2));
          cmd.Parameters.Add(DbOutParam("out_errmsg", OracleDbType.Varchar2, 4000));

          cmd.ExecuteProcedureWithCheckOut();
          connection.Close();
        }
      }
    }

    internal static int Get_UserData(string username, out string user_public_key, out int id_user) {
      int out_idpartner = 0;
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.StoredProcedure;

          cmd.CommandText = "API.get_user_public_key";
          cmd.Parameters.Add(DbParam("in_username", OracleDbType.Varchar2, username));
          cmd.Parameters.Add(DbOutParam("out_public_key", OracleDbType.Varchar2, 1024));
          cmd.Parameters.Add(DbOutParam("out_iduser", OracleDbType.Int32, 8));
          cmd.Parameters.Add(DbOutParam("out_idpartner", OracleDbType.Int32, 8));

          cmd.ExecuteProcedure();
          user_public_key = cmd.Parameters.GetStringValue("out_public_key");
          out_idpartner = cmd.Parameters.GetIntValue("out_idpartner");
          id_user = cmd.Parameters.GetIntValue("out_iduser");

          connection.Close();
        }
      }

      return out_idpartner;
    }

    internal static IList<ApiUserExtended> Get_ApiUsers() {
      IList<ApiUserExtended> result = new List<ApiUserExtended>();
      using (OracleConnection connection = new OracleConnection(Globals.oracle_cs)) {
        connection.Open();
        using (OracleCommand cmd = connection.CreateCommand()) {
          cmd.CommandType = CommandType.Text;

          cmd.CommandText = "select * from table(API.get_api_users())";

          try {
            OracleDataReader reader = cmd.ExecuteReader();

            if (reader.HasRows) {
              while (reader.Read()) {
                ApiUserExtended user = new ApiUserExtended() {
                  PublicKey = reader.GetStringValue("public_key"),
                  User_ID = reader.GetIntValue("id")
                };
                user.SetUsername(reader.GetStringValue("username"));
                result.Add(user);
              }
            }
          } catch (OracleException oe) {
            Console.Out.WriteLine($"OracleException{oe.Message}");
          }
          connection.Close();
        }
      }

      return result;
    }
  }

  #region base DataAccess
  internal static partial class DataAccess {
    private static string GetStringValue(this OracleParameterCollection parameter, string parameterName) {
      string response = string.Empty;
      try {
        response = parameter[parameterName].Value.ToString();
        if (response == "null") {
          response = string.Empty;
        }
      } catch (Exception e) {
        Console.Out.WriteLine($"{Tool.GetCurrentMethod()} exception: {e.Message}");
      }

      return response;
    }

    private static int GetIntValue(this OracleParameterCollection parameter, string parameterName) {
      if (int.TryParse(parameter[parameterName].Value.ToString(), out int response) == false) {
        Console.Out.WriteLine($"TryParse parameter: {parameterName}");
        throw new ApiException(CodeStatus.Unhandled_Exception);
      }
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
      int.TryParse(reader.GetValue(reader.GetOrdinal(name: parameter)).ToString(), out int response);
      return response;
    }

    private static decimal GetDecimalValue(this OracleDataReader reader, string parameter) {
      decimal.TryParse(reader.GetValue(reader.GetOrdinal(name: parameter)).ToString(), out decimal response);
      return response;
    }

    private static string GetStringValue(this OracleDataReader reader, string parameter) {
      return reader.GetValue(reader.GetOrdinal(name: parameter)).ToString();
    }

    private static DateTime GetDateTimeValue(this OracleDataReader reader, string parameter) {
      DateTime.TryParse(reader.GetValue(reader.GetOrdinal(name: parameter)).ToString(), out DateTime response);
      return response;
    }

    private static bool GetBooleanValue(this OracleDataReader reader, string parameter) {
      bool.TryParse(reader.GetValue(reader.GetOrdinal(name: parameter)).ToString(), out bool response);
      return response;
    }

    private static bool ExecuteProcedure(this OracleCommand command) {
      bool result = false;

      try {
        command.ExecuteNonQuery();
        result = true;

        if (command.Transaction != null) {
          command.Transaction.Commit();
        }
      } catch (OracleException oe) {
        Console.Out.WriteLine($"OracleException: command {command.CommandText} => {oe.Message}");
        result = false;
        if (command.Transaction != null) {
          command.Transaction.Rollback();
        }
      }

      return result;
    }

    private static bool ExecuteProcedureWithCheckOut(this OracleCommand command) {
      bool result = command.ExecuteProcedure();
      if (result == false) {
        throw new Exception("Stored procedure call failed");
      }

      string errmsg = command.Parameters.GetStringValue("out_errmsg");
      int errnum = command.Parameters.GetIntValue("out_errnum");

      if (errnum == 0) {
        return result;
      }

      Console.Out.WriteLine($"OracleException: command {command.CommandText} => {errnum}: {errmsg}");

      if (errnum == 2) {
        throw new ApiException(CodeStatus.Warning, errmsg);
      } else if (Globals.SafeStatuses.ContainsKey((CodeStatus)errnum) == true) {
        throw new ApiException((CodeStatus)errnum);
      } else {
        throw new ApiException(CodeStatus.Unhandled_Exception);
      }
    }

    private static void ThrowFrom(this OracleException exception, string command, string method) {
      Console.Out.WriteLine($"Command {command} OracleException: {exception.Message}");
      throw new ApiException((exception.ErrorCode == -20001) ? exception.Message : $"An error occurred during the operation {method}");
    }
  }
  #endregion base DataAccess
}