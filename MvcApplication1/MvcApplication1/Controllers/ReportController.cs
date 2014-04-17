﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Microsoft.AnalysisServices.AdomdClient;
using System.Configuration;
using System.Text;
using MvcApplication1.Models;
using System.Reflection;
using Microsoft.Office.Interop.Excel;
using System.Reflection;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using SQL = System.Data;
using Excel = Microsoft.Office.Interop.Excel;
using Microsoft.Office.Interop.Excel;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]

namespace MvcApplication1.Controllers
{
    public class ReportController : Controller
    {

        private static readonly log4net.ILog log = log4net.LogManager.GetLogger
    (System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        //
        // GET: /Report/

        public ActionResult ViewReport()
        {
            return View();
        }

        public ActionResult ViewRecipients(int notificationID = 0, int extractID = 0, string sentTo = "", string notificationDesc = "", string pnr = "")
        {
            log.Info("In ViewRecipients");
            RecipientModel recipientModel = RecipientsReport(notificationID, extractID, sentTo);
            recipientModel.NotificationName = notificationDesc;
            recipientModel.PNR = pnr;
            recipientModel.ExtractID = extractID;
            recipientModel.NotificationID = notificationID;
            return View("ViewRecipients", recipientModel);
        }

        public ActionResult ViewFlights(int flightID = 0, int extractID = 0, string pnr = "")
        {
            log.Info("In ViewFlights");
            FlightsModel flightsModel = FlightsReport(flightID, extractID);
            flightsModel.PNR = pnr;
            return View("ViewFlights", flightsModel);
        }

        public ActionResult ViewPax(int paxID = 0, int extractID = 0, string pnr = "")
        {
            log.Info("In ViewPax");
            PaxModel paxModel = PaxReport(paxID, extractID);
            paxModel.PNR = pnr;
            return View("ViewPax", paxModel);
        }

        public ActionResult ViewNotifications(int notificationID = 0, int dataSourceID = 0, string notificationDesc = "", int recipientID = 0, int extractID = 0, string dataSourceName = "", string pnr = "")
        {
            //get notifications by notification ID
            log.Info("In ViewNotifications");
            NotificationsReportModel notificationsModel = NotificationsReport(notificationID, dataSourceID, notificationDesc, recipientID, extractID);
            notificationsModel.DataSourceName = dataSourceName;
            notificationsModel.PNR = pnr;
            notificationsModel.DataSourceID = dataSourceID;
            notificationsModel.ExtractID = extractID;
            return View("ViewNotifications", notificationsModel);
        }

        public ActionResult ViewExtracts(int dataSourceID = 0, int recipientID = 0, string dataSourceName = "")
        {
            log.Info("In ViewExtracts");
            ExtractsModel extractsModel = ExtractsReport(dataSourceID, recipientID);
            extractsModel.DataSourceName = dataSourceName;
            extractsModel.DataSourceID = dataSourceID;
            extractsModel.DataSourceName = dataSourceName;
            extractsModel.RecipientID = recipientID;
            return View("ViewExtracts", extractsModel);
        }

        public ActionResult ViewDataSources(int notificationID = 0, int extractID = 0, string datasourcename = "", string notificationDesc = "", string pnr = "")
        {
            log.Info("In ViewDataSources");
            DataSourcesModel dataSourcesModel = DataSourcesReport(notificationID, datasourcename, extractID);
            dataSourcesModel.NotificationName = notificationDesc;
            dataSourcesModel.PNR = pnr;
            return View("ViewDataSources", dataSourcesModel);
        }

        [ValidateInput(false)]
        public ActionResult Search(string notificationdesc = "", string pnr = "", string sentto = "", string datasourcename = "")
        {
            log.Info("Controller: Search");
            log.Debug("notificationdesc: " + notificationdesc);
            log.Debug("pnr: " + pnr);
            log.Debug("sentto: " + sentto);
            log.Debug("datasourcename: " + datasourcename);

            if (notificationdesc != "")
            {
                //search by notification description
                NotificationsReportModel notificationsModel = NotificationsReport(0, 0, notificationdesc);
                return View("ViewNotifications", notificationsModel);
            }
            else if (pnr != "")
            {
                //search by PNR
                ExtractsModel extractsModel = ExtractsReport(0, 0, pnr);
                return View("ViewExtracts", extractsModel);
            }
            else if (sentto != "")
            {
                //search by sentto
                RecipientModel recipientModel = RecipientsReport(0, 0, sentto);
                return View("ViewRecipients", recipientModel);
            }
            else if (datasourcename != "")
            {
                //search by data source name
                DataSourcesModel dataSourcesModel = DataSourcesReport(0, datasourcename);
                return View("ViewDataSources", dataSourcesModel);
            }
            else
            {
                NotificationsReportModel notificationsModel = NotificationsReport();
                return View("ViewNotifications", notificationsModel);
            }
        }


        //make connection to SSAS and get back some data
        public NotificationsReportModel NotificationsReport(int notificationID = 0, int dataSourceID = 0, string notificationDesc = "", int recipientID = 0, int extractID = 0)
        {
            NotificationsReportModel model = new NotificationsReportModel();
            log.Info("Retrieving Notifications");
            model.NotificationID = new List<string>();
            model.NotificationDescription = new List<string>();
            model.SendDateTime = new List<string>();
            model.RecipientCount = new List<int>();
            model.LanguageType = new List<string>();
            using (AdomdConnection con = new AdomdConnection(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString))
            {
                log.Debug("Opening connection to OLAP to retrieve data");
                try
                {
                    con.Open();
                    String mdxQuery = "";
                    if (notificationID != 0)
                    {
                        //MDX query by notificationID
                        log.Debug("Get Notifications by NotificationID");
                        mdxQuery = "SELECT TOPCOUNT(ORDER({[Notifications Dim].[Notification ID].[" + notificationID + "] *[Notifications Dim].[Notification Desc].[Notification Desc].members * [Notifications Dim].[Send Date].[Send Date].members *[Notifications Dim].[Language Type].[Language Type].members},[Notifications Dim].[Notification ID], desc), 10) ON ROWS, {[Measures].[Recipients Dim Count]}ON COLUMNS FROM [Data Warehouse]";
                    }
                    else if (dataSourceID != 0)
                    {
                        //MDX query by datasourceID
                        log.Debug("Get Notifications by DataSourceID");
                        mdxQuery = "SELECT NONEMPTY(TOPCOUNT(ORDER({[Notifications Dim].[Notification ID].[Notification ID].members *[Notifications Dim].[Notification Desc].[Notification Desc].members * [Notifications Dim].[Send Date].[Send Date].members *[Notifications Dim].[Language Type].[Language Type].members},[Notifications Dim].[Notification ID], desc), 10)) ON ROWS, {[Measures].[Recipients Dim Count]}ON COLUMNS FROM [Data Warehouse] WHERE [Data Source Dim].[Data Source ID].&[" + dataSourceID + "]";
                    }
                    else if (recipientID != 0)
                    {
                        //MDX query by recipientID
                        log.Debug("Get Notifications by RecipientID");
                        mdxQuery = "SELECT NONEMPTY (TOPCOUNT(ORDER({[Notifications Dim].[Notification ID].[Notification ID].members* [Notifications Dim].[Notification Desc].[Notification Desc].members * [Notifications Dim].[Send Date].[Send Date].members *[Notifications Dim].[Language Type].[Language Type].members},[Notifications Dim].[Notification ID], desc), 10) )ON ROWS, {[Measures].[Recipients Dim Count]} ON COLUMNS FROM [Data Warehouse] WHERE [Recipients Dim].[Recipient ID].&[" + recipientID + "]";
                    }
                    else if (notificationDesc != "")
                    {
                        //MDX query by notification description
                        log.Debug("Get Notifications by NotificationDesc");
                        mdxQuery = "SELECT NON EMPTY { [Measures].[Recipients Dim Count] } ON COLUMNS, NON EMPTY FILTER ({[Notifications Dim].[Notification ID].[Notification ID].members * [Notifications Dim].[Notification Desc].[Notification Desc].members *[Notifications Dim].[Send Date].[Send Date].members *[Notifications Dim].[Language Type].[Language Type].members},instr([Notifications Dim].[Notification Desc].currentmember.member_caption,'" + notificationDesc + "')>0) ON ROWS FROM [Data Warehouse]";
                    }
                    else if (extractID != 0)
                    {
                        //MDX query by extractID
                        log.Debug("Get Notifications by extractID");
                        mdxQuery = "SELECT NONEMPTY(TOPCOUNT(ORDER({[Notifications Dim].[Notification ID].[Notification ID].members *[Notifications Dim].[Notification Desc].[Notification Desc].members * [Notifications Dim].[Send Date].[Send Date].members *[Notifications Dim].[Language Type].[Language Type].members},[Notifications Dim].[Notification ID], desc), 10)) ON ROWS, [Measures].[Recipients Dim Count]ON COLUMNS FROM [Data Warehouse]WHERE [Extracts Dim].[Extractid].&[" + extractID + "]";
                    }
                    else
                    {
                        //MDX query top 10
                        log.Debug("Get Notifications by top 10");
                        mdxQuery = "SELECT NONEMPTY(TOPCOUNT(ORDER({[Notifications Dim].[Notification ID].[Notification ID].members* [Notifications Dim].[Notification Desc].[Notification Desc].members * [Notifications Dim].[Send Date].[Send Date].members *[Notifications Dim].[Language Type].[Language Type].members},[Notifications Dim].[Notification ID], desc), 10)) ON ROWS, {[Measures].[Recipients Dim Count]}ON COLUMNS FROM [Data Warehouse]";
                    }
                    using (AdomdCommand command = new AdomdCommand(mdxQuery, con))
                    {
                        try
                        {
                            using (AdomdDataReader reader = command.ExecuteReader())
                            {
                                int recordNumber = 0;
                                while (reader.Read())
                                {
                                    log.Info("Reading Record #: " + recordNumber);
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        log.Debug("Reading Field #: " + i);
                                        log.Debug("Returned Field: " + reader[i]);

                                        //field in records coming back in the following order:
                                        //0 - NotificationID
                                        //1 - NotificationDescription
                                        //2 - SendDateTime
                                        //3 - RecipientCount
                                        //4 - LanguageType
                                        //5 - DataSourceName

                                        //add the data set returned to the model
                                        switch (i)
                                        {
                                            case 0:
                                                model.NotificationID.Add((string)reader[i]);
                                                break;
                                            case 1:
                                                model.NotificationDescription.Add((string)reader[i]);
                                                break;
                                            case 2:
                                                model.SendDateTime.Add((string)reader[i]);
                                                break;
                                            case 3:
                                                model.LanguageType.Add((string)reader[i]);
                                                break;
                                            case 4:
                                                model.RecipientCount.Add((int)reader[i]);
                                                break;
                                        }
                                    }
                                    recordNumber++;
                                }
                            }
                        }
                        catch (AdomdErrorResponseException ex)
                        {
                            log.Error("ERROR when trying to read recrods");
                            log.Error(ex);
                        }
                    }
                    con.Close();
                }
                catch (Microsoft.AnalysisServices.AdomdClient.AdomdConnectionException ex)
                {
                    log.Error(ex);
                }
            }
            return model;
        }

        public RecipientModel RecipientsReport(int notificationID = 0, int extractID = 0, string sentTo = "", bool export = false)
        {
            RecipientModel model = new RecipientModel();
            model.RecipientID = new List<string>();
            model.SentTo = new List<string>();
            model.SendDateTime = new List<string>();
            model.NotificationType = new List<string>();
            model.TemplateName = new List<string>();
            using (AdomdConnection con = new AdomdConnection(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString))
            {
                log.Debug("Opening connection to OLAP to retrieve data");
                try
                {
                    con.Open();
                    String mdxQuery = "";
                    if (notificationID != 0)
                    {
                        //MDX query by notificationID
                        log.Debug("Get Recipients by NotificationID");
                        if (export == false)
                        {
                            mdxQuery = "SELECT [Measures].[Recipients Dim Count]ON COLUMNS,NON EMPTY TOPCOUNT({[Recipients Dim].[Recipient ID].[Recipient ID].members *[Recipients Dim].[Recipient Sent To].[Recipient Sent To].members *[Recipients Dim].[Recipient Sent Date Time].[Recipient Sent Date Time].members *[Recipients Dim].[Sent Type].[Sent Type].members *[Templates Dim].[Template Desc].[Template Desc].members}, 10, [Recipients Dim].[Recipient ID])	ON ROWS FROM [Data Warehouse] WHERE [Notifications Dim].[Notification ID].&[" + notificationID + "]";
                        }
                        else
                        {
                            mdxQuery = "SELECT [Measures].[Recipients Dim Count]ON COLUMNS,NON EMPTY {[Recipients Dim].[Recipient ID].[Recipient ID].members *[Recipients Dim].[Recipient Sent To].[Recipient Sent To].members *[Recipients Dim].[Recipient Sent Date Time].[Recipient Sent Date Time].members *[Recipients Dim].[Sent Type].[Sent Type].members *[Templates Dim].[Template Desc].[Template Desc].members}	ON ROWS FROM [Data Warehouse] WHERE [Notifications Dim].[Notification ID].&[" + notificationID + "]";
                        }
                    }
                    else if (extractID != 0)
                    {
                        //MDX query by extractID
                        log.Debug("Get Recipients by extractID");
                        if (export == false)
                        {
                            mdxQuery = "SELECT [Measures].[Recipients Dim Count]ON COLUMNS,NON EMPTY TOPCOUNT({[Recipients Dim].[Recipient ID].[Recipient ID].members *[Recipients Dim].[Recipient Sent To].[Recipient Sent To].members *[Recipients Dim].[Recipient Sent Date Time].[Recipient Sent Date Time].members *[Recipients Dim].[Sent Type].[Sent Type].members *[Templates Dim].[Template Desc].[Template Desc].members}, 10, [Recipients Dim].[Recipient ID])	ON ROWS FROM [Data Warehouse] WHERE [Extracts Dim].[Extractid].&[" + extractID + "]";
                        }
                        else
                        {
                            mdxQuery = "SELECT [Measures].[Recipients Dim Count]ON COLUMNS,NON EMPTY {[Recipients Dim].[Recipient ID].[Recipient ID].members *[Recipients Dim].[Recipient Sent To].[Recipient Sent To].members *[Recipients Dim].[Recipient Sent Date Time].[Recipient Sent Date Time].members *[Recipients Dim].[Sent Type].[Sent Type].members *[Templates Dim].[Template Desc].[Template Desc].members} ON ROWS FROM [Data Warehouse] WHERE [Extracts Dim].[Extractid].&[" + extractID + "]";
                        }
                    }
                    else if (sentTo != "")
                    {
                        //MDX query by sentTo
                        log.Debug("Get Recipients by sentTo");
                        if (export == false)
                        {
                            mdxQuery = "SELECT [Measures].[Recipients Dim Count]ON COLUMNS,NON EMPTY TOPCOUNT(FILTER({[Recipients Dim].[Recipient ID].[Recipient ID].members *[Recipients Dim].[Recipient Sent To].[Recipient Sent To].members *[Recipients Dim].[Recipient Sent Date Time].[Recipient Sent Date Time].members *[Recipients Dim].[Sent Type].[Sent Type].members*[Templates Dim].[Template Desc].[Template Desc].members},instr([Recipients Dim].[Recipient Sent To].currentmember.member_caption,'" + sentTo + "')>0), 10, [Recipients Dim].[Recipient ID])	ON ROWS FROM [Data Warehouse]";
                        }
                        else
                        {
                            mdxQuery = "SELECT [Measures].[Recipients Dim Count]ON COLUMNS,NON EMPTY FILTER({[Recipients Dim].[Recipient ID].[Recipient ID].members *[Recipients Dim].[Recipient Sent To].[Recipient Sent To].members *[Recipients Dim].[Recipient Sent Date Time].[Recipient Sent Date Time].members *[Recipients Dim].[Sent Type].[Sent Type].members*[Templates Dim].[Template Desc].[Template Desc].members},instr([Recipients Dim].[Recipient Sent To].currentmember.member_caption,'" + sentTo + "')>0)	ON ROWS FROM [Data Warehouse]";
                        }
                    }
                    else
                    {
                        //MDX get top 10
                        log.Debug("Get Recipients by top 10");
                        mdxQuery = "SELECT [Measures].[Recipients Dim Count] ON COLUMNS, NON EMPTY TOPCOUNT({[Recipients Dim].[Recipient ID].[Recipient ID].members *[Recipients Dim].[Recipient Sent To].[Recipient Sent To].members *[Recipients Dim].[Recipient Sent Date Time].[Recipient Sent Date Time].members *[Recipients Dim].[Sent Type].[Sent Type].members *[Templates Dim].[Template Desc].[Template Desc].members}, 10, [Recipients Dim].[Recipient ID])	ON ROWS FROM [Data Warehouse]";
                    }
                    using (AdomdCommand command = new AdomdCommand(mdxQuery, con))
                    {
                        try
                        {
                            using (AdomdDataReader reader = command.ExecuteReader())
                            {
                                int recordNumber = 0;
                                while (reader.Read())
                                {
                                    log.Info("Reading Record #: " + recordNumber);
                                    {
                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            log.Debug("Reading Field #: " + i);
                                            log.Debug("Returned Field: " + reader[i]);

                                            //add the data set returned to the model
                                            switch (i)
                                            {
                                                case 0:
                                                    model.RecipientID.Add((string)reader[i]);
                                                    break;
                                                case 1:
                                                    model.SentTo.Add((string)reader[i]);
                                                    break;
                                                case 2:
                                                    model.SendDateTime.Add((string)reader[i]);
                                                    break;
                                                case 3:
                                                    model.NotificationType.Add((string)reader[i]);
                                                    break;
                                                case 4:
                                                    model.TemplateName.Add((string)reader[i]);
                                                    break;
                                            }
                                        }
                                    }
                                    recordNumber++;
                                }
                            }
                        }
                        catch (AdomdErrorResponseException ex)
                        {
                            log.Error("ERROR when trying to read recrods");
                            log.Error(ex);
                        }
                    }
                    con.Close();
                }
                catch (Microsoft.AnalysisServices.AdomdClient.AdomdConnectionException ex)
                {
                    log.Error(ex);
                }
            }
            return model;
        }

        public ExtractsModel ExtractsReport(int dataSourceID = 0, int recipientID = 0, string pnr = "", bool export = false)
        {
            ExtractsModel model = new ExtractsModel();
            log.Debug("Retrieving Extracts");
            model.ExtractID = new List<string>();
            model.PNR = new List<string>();
            model.LeadPaxName = new List<string>();
            model.TotalPax = new List<string>();
            model.UploadDateTime = new List<string>();
            model.BookingDate = new List<string>();
            model.EmailAddress = new List<string>();
            model.OtherPhone = new List<string>();
            model.LanguageCode = new List<string>();
            model.NumberofFlights = new List<int>();
            using (AdomdConnection con = new AdomdConnection(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString))
            {
                log.Debug("Opening connection to OLAP to retrieve data");
                try
                {
                    con.Open();
                    String mdxQuery = "";
                    if (dataSourceID != 0)
                    {
                        //MDX query by data source ID
                        log.Debug("Get Extracts by data source ID");
                        if (export == false)
                        {
                            mdxQuery = "SELECT [Measures].[Flights Dim Count]ON COLUMNS,NON EMPTY TOPCOUNT({[Extracts Dim].[Extractid].[Extractid].members *[Extracts Dim].[PNR].[PNR].members *[Extracts Dim].[Address Name].[Address Name].members *[Extracts Dim].[Total Pax].[Total Pax].members *[Extracts Dim].[Date Added].[Date Added].members *[Extracts Dim].[Booking Date].[Booking Date].members * [Extracts Dim].[Email Address].[Email Address].members * [Extracts Dim].[Other Phone].[Other Phone].members * [Extracts Dim].[Language Code].[Language Code].members}, 10, [Extracts Dim].[Extractid])	ON ROWS FROM [Data Warehouse] WHERE [Data Source Dim].[Data Source ID].&[" + dataSourceID + "]";
                        }
                        else
                        {
                            mdxQuery = "SELECT [Measures].[Flights Dim Count]ON COLUMNS,NON EMPTY {[Extracts Dim].[Extractid].[Extractid].members *[Extracts Dim].[PNR].[PNR].members *[Extracts Dim].[Address Name].[Address Name].members *[Extracts Dim].[Total Pax].[Total Pax].members *[Extracts Dim].[Date Added].[Date Added].members *[Extracts Dim].[Booking Date].[Booking Date].members * [Extracts Dim].[Email Address].[Email Address].members * [Extracts Dim].[Other Phone].[Other Phone].members * [Extracts Dim].[Language Code].[Language Code].members}	ON ROWS FROM [Data Warehouse] WHERE [Data Source Dim].[Data Source ID].&[" + dataSourceID + "]";
                        }
                    }
                    else if (recipientID != 0)
                    {
                        //MDX query by recipientID
                        log.Debug("Get Extracts by recipientID");
                        if (export == false)
                        {
                            mdxQuery = "SELECT [Measures].[Flights Dim Count]ON COLUMNS,NON EMPTY TOPCOUNT({[Extracts Dim].[Extractid].[Extractid].members *[Extracts Dim].[PNR].[PNR].members *[Extracts Dim].[Address Name].[Address Name].members *[Extracts Dim].[Total Pax].[Total Pax].members *[Extracts Dim].[Date Added].[Date Added].members *[Extracts Dim].[Booking Date].[Booking Date].members * [Extracts Dim].[Email Address].[Email Address].members * [Extracts Dim].[Other Phone].[Other Phone].members * [Extracts Dim].[Language Code].[Language Code].members}, 10, [Extracts Dim].[Extractid])	ON ROWS FROM [Data Warehouse] WHERE [Recipients Dim].[Recipient ID].&[" + recipientID + "]";
                        }
                        else
                        {
                            mdxQuery = "SELECT [Measures].[Flights Dim Count]ON COLUMNS,NON EMPTY {[Extracts Dim].[Extractid].[Extractid].members *[Extracts Dim].[PNR].[PNR].members *[Extracts Dim].[Address Name].[Address Name].members *[Extracts Dim].[Total Pax].[Total Pax].members *[Extracts Dim].[Date Added].[Date Added].members *[Extracts Dim].[Booking Date].[Booking Date].members * [Extracts Dim].[Email Address].[Email Address].members * [Extracts Dim].[Other Phone].[Other Phone].members * [Extracts Dim].[Language Code].[Language Code].members}	ON ROWS FROM [Data Warehouse] WHERE [Recipients Dim].[Recipient ID].&[" + recipientID + "]";
                        }
                    }
                    else if (pnr != "")
                    {
                        //MDX query by PNR
                        log.Debug("Get Extracts by PNR");
                        if (export == false)
                        {
                            mdxQuery = "SELECT [Measures].[Flights Dim Count]ON COLUMNS,NON EMPTY TOPCOUNT(FILTER({[Extracts Dim].[Extractid].[Extractid].members *[Extracts Dim].[PNR].[PNR].members *[Extracts Dim].[Address Name].[Address Name].members *[Extracts Dim].[Total Pax].[Total Pax].members *[Extracts Dim].[Date Added].[Date Added].members*[Extracts Dim].[Booking Date].[Booking Date].members * [Extracts Dim].[Email Address].[Email Address].members * [Extracts Dim].[Other Phone].[Other Phone].members * [Extracts Dim].[Language Code].[Language Code].members},instr([Extracts Dim].[PNR].currentmember.member_caption,'" + pnr + "')>0), 10, [Extracts Dim].[Extractid])	ON ROWS FROM [Data Warehouse]";
                        }
                        else
                        {
                            mdxQuery = "SELECT [Measures].[Flights Dim Count]ON COLUMNS,NON EMPTY FILTER({[Extracts Dim].[Extractid].[Extractid].members *[Extracts Dim].[PNR].[PNR].members *[Extracts Dim].[Address Name].[Address Name].members *[Extracts Dim].[Total Pax].[Total Pax].members *[Extracts Dim].[Date Added].[Date Added].members*[Extracts Dim].[Booking Date].[Booking Date].members * [Extracts Dim].[Email Address].[Email Address].members * [Extracts Dim].[Other Phone].[Other Phone].members * [Extracts Dim].[Language Code].[Language Code].members},instr([Extracts Dim].[PNR].currentmember.member_caption,'" + pnr + "')>0)	ON ROWS FROM [Data Warehouse]";
                        }
                    }
                    else
                    {
                        //MDX top 10
                        log.Debug("Get Extracts by top 10");
                        mdxQuery = "SELECT [Measures].[Flights Dim Count]ON COLUMNS,NON EMPTY TOPCOUNT({[Extracts Dim].[Extractid].[Extractid].members *[Extracts Dim].[PNR].[PNR].members *[Extracts Dim].[Address Name].[Address Name].members *[Extracts Dim].[Total Pax].[Total Pax].members *[Extracts Dim].[Date Added].[Date Added].members *[Extracts Dim].[Booking Date].[Booking Date].members * [Extracts Dim].[Email Address].[Email Address].members * [Extracts Dim].[Other Phone].[Other Phone].members * [Extracts Dim].[Language Code].[Language Code].members}, 10, [Extracts Dim].[Extractid])	ON ROWS FROM [Data Warehouse]";
                    }
                    using (AdomdCommand command = new AdomdCommand(mdxQuery, con))
                    {
                        try
                        {
                            using (AdomdDataReader reader = command.ExecuteReader())
                            {
                                int recordNumber = 0;
                                while (reader.Read())
                                {
                                    log.Info("Reading Record #: " + recordNumber);
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        log.Debug("Reading Field #: " + i);
                                        log.Debug("Returned Field: " + reader[i]);

                                        //add the data set returned to the model
                                        switch (i)
                                        {
                                            case 0:
                                                model.ExtractID.Add((string)reader[i]);
                                                break;
                                            case 1:
                                                model.PNR.Add((string)reader[i]);
                                                break;
                                            case 2:
                                                model.LeadPaxName.Add((string)reader[i]);
                                                break;
                                            case 3:
                                                model.TotalPax.Add((string)reader[i]);
                                                break;
                                            case 4:
                                                model.UploadDateTime.Add((string)reader[i]);
                                                break;
                                            case 5:
                                                model.BookingDate.Add((string)reader[i]);
                                                break;
                                            case 6:
                                                model.EmailAddress.Add((string)reader[i]);
                                                break;
                                            case 7:
                                                model.OtherPhone.Add((string)reader[i]);
                                                break;
                                            case 8:
                                                model.LanguageCode.Add((string)reader[i]);
                                                break;
                                            case 9:
                                                model.NumberofFlights.Add((int)reader[i]);
                                                break;
                                        }
                                    }
                                }
                                recordNumber++;
                            }
                        }

                        catch (AdomdErrorResponseException ex)
                        {
                            log.Error("ERROR when trying to read recrods");
                            log.Error(ex);
                        }
                    }
                    con.Close();
                }
                catch (Microsoft.AnalysisServices.AdomdClient.AdomdConnectionException ex)
                {
                    log.Error(ex);
                }
            }
            return model;
        }

        public DataSourcesModel DataSourcesReport(int notificationID = 0, string datasourcename = "", int extractID = 0)
        {
            DataSourcesModel model = new DataSourcesModel();
            log.Debug("Retrieving Data Sources");
            model.DataSourceID = new List<string>();
            model.DataSourceName = new List<string>();
            model.ExtractsCount = new List<int>();
            model.UploadDateTime = new List<string>();
            model.DataSourceType = new List<string>();
            using (AdomdConnection con = new AdomdConnection(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString))
            {
                log.Debug("Opening connection to OLAP to retrieve data");
                try
                {
                    con.Open();
                    String mdxQuery = "";
                    if (notificationID != 0)
                    {
                        //MDX query by notificationID
                        log.Debug("Get Data Sources by notificationID");
                        mdxQuery = "SELECT [Measures].[Extracts Dim Count]ON COLUMNS,NON EMPTY TOPCOUNT({[Data Source Dim].[Data Source ID].[Data Source ID].members *[Data Source Dim].[Data Source Name].[Data Source Name].members *[Data Source Dim].[Create Date].[Create Date].members *[Data Source Dim].[Data Source Type].[Data Source Type].members}, 10, [Data Source Dim].[Data Source ID])	ON ROWS FROM [Data Warehouse] WHERE [Notifications Dim].[Notification ID].&[" + notificationID + "]";
                    }
                    else if (extractID != 0)
                    {
                        //MDX query by extractID
                        log.Debug("Get Data Sources by extractID");
                        mdxQuery = "SELECT [Measures].[Extracts Dim Count] ON COLUMNS,NON EMPTY TOPCOUNT({[Data Source Dim].[Data Source ID].[Data Source ID].members *[Data Source Dim].[Data Source Name].[Data Source Name].members *[Data Source Dim].[Create Date].[Create Date].members *[Data Source Dim].[Data Source Type].[Data Source Type].members}, 10, [Data Source Dim].[Data Source ID])	ON ROWS FROM [Data Warehouse] WHERE [Extracts Dim].[Extractid].&[" + extractID + "]";
                    }
                    else if (datasourcename != "")
                    {
                        //MDX query by data source name
                        log.Debug("Get Data Sources by data source name");
                        mdxQuery = "SELECT [Measures].[Extracts Dim Count]ON COLUMNS,NON EMPTY TOPCOUNT(FILTER({[Data Source Dim].[Data Source ID].[Data Source ID].members *[Data Source Dim].[Data Source Name].[Data Source Name].members *[Data Source Dim].[Create Date].[Create Date].members *[Data Source Dim].[Data Source Type].[Data Source Type].members},instr([Data Source Dim].[Data Source Name].currentmember.member_caption,'" + datasourcename + "')>0), 10, [Data Source Dim].[Data Source ID])	ON ROWS FROM [Data Warehouse]";
                    }
                    else
                    {
                        //MDX top 10
                        log.Debug("Get Data Sources by top 10");
                        mdxQuery = "SELECT [Measures].[Extracts Dim Count]ON COLUMNS,NON EMPTY TOPCOUNT({[Data Source Dim].[Data Source ID].[Data Source ID].members *[Data Source Dim].[Data Source Name].[Data Source Name].members *[Data Source Dim].[Create Date].[Create Date].members *[Data Source Dim].[Data Source Type].[Data Source Type].members}, 10, [Data Source Dim].[Data Source ID])	ON ROWS FROM [Data Warehouse]";
                    }
                    using (AdomdCommand command = new AdomdCommand(mdxQuery, con))
                    {
                        try
                        {
                            using (AdomdDataReader reader = command.ExecuteReader())
                            {
                                int recordNumber = 0;
                                while (reader.Read())
                                {
                                    log.Info("Reading Record #: " + recordNumber);
                                    for (int i = 0; i < reader.FieldCount; i++)
                                    {
                                        log.Debug("Reading Field #: " + i);
                                        log.Debug("Returned Field: " + reader[i]);

                                        //field in records coming back in the following order:
                                        //0 - DataSourceID
                                        //1 - DataSourceName
                                        //2 - ExtractsCount
                                        //3 - UploadDateTime
                                        //4 - DataSourceType

                                        //add the data set returned to the model
                                        switch (i)
                                        {
                                            case 0:
                                                model.DataSourceID.Add((string)reader[i]);
                                                break;
                                            case 1:
                                                model.DataSourceName.Add((string)reader[i]);
                                                break;
                                            case 4:
                                                model.ExtractsCount.Add((int)reader[i]);
                                                break;
                                            case 2:
                                                model.UploadDateTime.Add((string)reader[i]);
                                                break;
                                            case 3:
                                                model.DataSourceType.Add((string)reader[i]);
                                                break;
                                        }
                                    }

                                    recordNumber++;
                                }
                            }
                        }
                        catch (AdomdErrorResponseException ex)
                        {
                            log.Error("ERROR when trying to read recrods");
                            log.Error(ex);
                        }
                    }
                    con.Close();
                }
                catch (Microsoft.AnalysisServices.AdomdClient.AdomdConnectionException ex)
                {
                    log.Error(ex);
                }
            }
            return model;
        }

        public FlightsModel FlightsReport(int flightID = 0, int extractID = 0)
        {
            FlightsModel model = new FlightsModel();
            model.FlightID = new List<string>();
            model.FlightLeg = new List<string>();
            model.FlightNumber = new List<string>();
            model.FlightCode = new List<string>();
            model.DepartureDate = new List<string>();
            model.DepartureTime = new List<string>();
            model.DepartureCountry = new List<string>();
            model.DepartureCity = new List<string>();
            model.ArrivalDate = new List<string>();
            model.ArrivalTime = new List<string>();
            model.ArrivalCountry = new List<string>();
            model.ArrivalCity = new List<string>();
            model.ScheduleChange = new List<string>();
            model.ServiceClass = new List<string>();
            model.Duration = new List<string>();
            using (AdomdConnection con = new AdomdConnection(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString))
            {
                log.Debug("Opening connection to OLAP to retrieve data");
                try
                {
                    con.Open();
                    String mdxQuery = "";
                    if (flightID != 0)
                    {
                        //MDX query by flightID
                        log.Debug("Get Flights by flightID");
                        mdxQuery = "SELECT NONEMPTY (ORDER({[Flights Dim].[Flight ID].[" + flightID + "] *[Flights Dim].[Flight Leg].[Flight Leg].members *[Flights Dim].[Flight Number].[Flight Number].members *[Flights Dim].[Flight Code].[Flight Code].members *[Flights Dim].[Departure Date].[Departure Date].members *[Flights Dim].[Departure Time].[Departure Time].members *[Flights Dim].[Departure Country].[Departure Country].members *[Flights Dim].[Departure City].[Departure City].members *[Flights Dim].[Arrival Date].[Arrival Date].members *[Flights Dim].[Arrival Time].[Arrival Time].members *[Flights Dim].[Arrival Country].[Arrival Country].members *[Flights Dim].[Arrival City].[Arrival City].members *[Flights Dim].[Schedule Change].[Schedule Change].members *[Flights Dim].[Service Class].[Service Class].members *[Flights Dim].[Duration].[Duration].members },[Flights Dim].[Flight Leg], asc) )ON ROWS, {[Measures].[Fact Table Count]} ON COLUMNS FROM [Data Warehouse] ";
                    }
                    else if (extractID != 0)
                    {
                        //MDX query by extractID
                        log.Debug("Get Flights by extractID");
                        mdxQuery = "SELECT NONEMPTY (ORDER({[Flights Dim].[Flight ID].[Flight ID] *[Flights Dim].[Flight Leg].[Flight Leg].members *[Flights Dim].[Flight Number].[Flight Number].members *[Flights Dim].[Flight Code].[Flight Code].members *[Flights Dim].[Departure Date].[Departure Date].members *[Flights Dim].[Departure Time].[Departure Time].members *[Flights Dim].[Departure Country].[Departure Country].members *[Flights Dim].[Departure City].[Departure City].members *[Flights Dim].[Arrival Date].[Arrival Date].members *[Flights Dim].[Arrival Time].[Arrival Time].members *[Flights Dim].[Arrival Country].[Arrival Country].members *[Flights Dim].[Arrival City].[Arrival City].members *[Flights Dim].[Schedule Change].[Schedule Change].members *[Flights Dim].[Service Class].[Service Class].members *[Flights Dim].[Duration].[Duration].members },[Flights Dim].[Flight Leg], asc) )ON ROWS, {[Measures].[Fact Table Count]} ON COLUMNS FROM [Data Warehouse] WHERE [Extracts Dim].[Extractid].&[" + extractID + "]";
                    }
                    using (AdomdCommand command = new AdomdCommand(mdxQuery, con))
                    {
                        try
                        {
                            using (AdomdDataReader reader = command.ExecuteReader())
                            {
                                int recordNumber = 0;
                                while (reader.Read())
                                {
                                    log.Info("Reading Record #: " + recordNumber);
                                    {
                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            log.Debug("Reading Field #: " + i);
                                            log.Debug("Returned Field: " + reader[i]);

                                            //add the data set returned to the model
                                            switch (i)
                                            {
                                                case 0:
                                                    model.FlightID.Add((string)reader[i]);
                                                    break;
                                                case 1:
                                                    model.FlightLeg.Add((string)reader[i]);
                                                    break;
                                                case 2:
                                                    model.FlightNumber.Add((string)reader[i]);
                                                    break;
                                                case 3:
                                                    model.FlightCode.Add((string)reader[i]);
                                                    break;
                                                case 4:
                                                    model.DepartureDate.Add((string)reader[i]);
                                                    break;
                                                case 5:
                                                    model.DepartureTime.Add((string)reader[i]);
                                                    break;
                                                case 6:
                                                    model.DepartureCountry.Add((string)reader[i]);
                                                    break;
                                                case 7:
                                                    model.DepartureCity.Add((string)reader[i]);
                                                    break;
                                                case 8:
                                                    model.ArrivalDate.Add((string)reader[i]);
                                                    break;
                                                case 9:
                                                    model.ArrivalTime.Add((string)reader[i]);
                                                    break;
                                                case 10:
                                                    model.ArrivalCountry.Add((string)reader[i]);
                                                    break;
                                                case 11:
                                                    model.ArrivalCity.Add((string)reader[i]);
                                                    break;
                                                case 12:
                                                    model.ScheduleChange.Add((string)reader[i]);
                                                    break;
                                                case 13:
                                                    model.ServiceClass.Add((string)reader[i]);
                                                    break;
                                                case 14:
                                                    model.Duration.Add((string)reader[i]);
                                                    break;
                                            }
                                        }
                                    }
                                    recordNumber++;
                                }
                            }
                        }
                        catch (AdomdErrorResponseException ex)
                        {
                            log.Error("ERROR when trying to read recrods");
                            log.Error(ex);
                        }
                    }
                    con.Close();
                }
                catch (Microsoft.AnalysisServices.AdomdClient.AdomdConnectionException ex)
                {
                    log.Error(ex);
                }
            }
            return model;
        }

        public PaxModel PaxReport(int PaxID = 0, int extractID = 0)
        {
            PaxModel model = new PaxModel();
            model.PaxID = new List<string>();
            model.PaxFirst = new List<string>();
            model.PaxMiddle = new List<string>();
            model.PaxLast = new List<string>();
            model.PaxTitle = new List<string>();
            model.PaxGender = new List<string>();
            model.PaxTitle = new List<string>();
            model.PaxDOB = new List<string>();
            model.PaxType = new List<string>();
            using (AdomdConnection con = new AdomdConnection(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString))
            {
                log.Debug("Opening connection to OLAP to retrieve data");
                try
                {
                    con.Open();
                    String mdxQuery = "";
                    if (PaxID != 0)
                    {
                        //MDX query by PaxID
                        log.Debug("Get Pax by PaxID");
                        mdxQuery = "SELECT NONEMPTY (ORDER({[Pax Dim].[Pax ID].[" + PaxID + "].members *[Pax Dim].[Pax First].[Pax First].members *[Pax Dim].[Pax Middle].[Pax Middle].members *[Pax Dim].[Pax Last].[Pax Last].members *[Pax Dim].[Pax Title].[Pax Title].members *[Pax Dim].[Pax Gender].[Pax Gender].members *[Pax Dim].[Pax Type].[Pax Type].members *[Pax Dim].[Pax DOB].[Pax DOB].members },[Pax Dim].[Pax ID], asc) )ON ROWS, {[Measures].[Fact Table Count]} ON COLUMNS FROM [Data Warehouse] ";
                    }
                    else if (extractID != 0)
                    {
                        //MDX query by extractID
                        log.Debug("Get Flights by extractID");
                        mdxQuery = "SELECT NONEMPTY (ORDER({[Pax Dim].[Pax ID].[Pax ID].members *[Pax Dim].[Pax First].[Pax First].members *[Pax Dim].[Pax Middle].[Pax Middle].members *[Pax Dim].[Pax Last].[Pax Last].members *[Pax Dim].[Pax Title].[Pax Title].members *[Pax Dim].[Pax Gender].[Pax Gender].members *[Pax Dim].[Pax Type].[Pax Type].members *[Pax Dim].[Pax DOB].[Pax DOB].members },[Pax Dim].[Pax ID], asc) )ON ROWS, {[Measures].[Fact Table Count]} ON COLUMNS FROM [Data Warehouse] WHERE [Extracts Dim].[Extractid].&[" + extractID + "]";
                    }
                    using (AdomdCommand command = new AdomdCommand(mdxQuery, con))
                    {
                        try
                        {
                            using (AdomdDataReader reader = command.ExecuteReader())
                            {
                                int recordNumber = 0;
                                while (reader.Read())
                                {
                                    log.Info("Reading Record #: " + recordNumber);
                                    {
                                        for (int i = 0; i < reader.FieldCount; i++)
                                        {
                                            log.Debug("Reading Field #: " + i);
                                            log.Debug("Returned Field: " + reader[i]);

                                            //add the data set returned to the model
                                            switch (i)
                                            {
                                                case 0:
                                                    model.PaxID.Add((string)reader[i]);
                                                    break;
                                                case 1:
                                                    model.PaxFirst.Add((string)reader[i]);
                                                    break;
                                                case 2:
                                                    model.PaxMiddle.Add((string)reader[i]);
                                                    break;
                                                case 3:
                                                    model.PaxLast.Add((string)reader[i]);
                                                    break;
                                                case 4:
                                                    model.PaxTitle.Add((string)reader[i]);
                                                    break;
                                                case 5:
                                                    model.PaxGender.Add((string)reader[i]);
                                                    break;
                                                case 6:
                                                    model.PaxType.Add((string)reader[i]);
                                                    break;
                                                case 7:
                                                    model.PaxDOB.Add((string)reader[i]);
                                                    break;
                                            }
                                        }
                                    }
                                    recordNumber++;
                                }
                            }
                        }
                        catch (AdomdErrorResponseException ex)
                        {
                            log.Error("ERROR when trying to read recrods");
                            log.Error(ex);
                        }
                    }
                    con.Close();
                }
                catch (Microsoft.AnalysisServices.AdomdClient.AdomdConnectionException ex)
                {
                    log.Error(ex);
                }
            }
            return model;
        }

        public void ExportNotifications(string DataSourceName = "", string PNR = "", int DataSourceID = 0, int extractID = 0)
        {
            log.Info("In EXPORT Notifications");
            NotificationsToCsv(DataSourceName, PNR, DataSourceID, extractID);
        }

        public void NotificationsToCsv(string DataSourceName = "", string PNR = "", int DataSourceID = 0, int extractID = 0)
        {
            string filename = "";
            if (DataSourceID != 0)
            {
                //notifications for data source
                filename = "Notifications for Data Source " + DataSourceName + ".csv";
            }
            else if (extractID != 0)
            {
                //notifications for a PNR
                filename = "Notifications for PNR " + PNR + ".csv";
            }
            else
            {
                filename = "Recent Notifications.csv";
            }
            string path = ConfigurationManager.AppSettings["ExportsPath"];
            path += filename;
            log.Debug("Exports File Directory: " + path);
            NotificationsReportModel notificationsModel = NotificationsReport(0, DataSourceID,"",0,extractID);
            log.Debug("MODEL.NotificationID[0]: " + notificationsModel.NotificationID[0]);
            //headers for the csv file
            string headers = "Notification Description, Send Date Time, Recipient Count, Language Type";
            try
            {
                string csv = "";
                csv += headers;
                for (int i = 0; i < notificationsModel.NotificationID.Count; i++)
                {
                        csv += Environment.NewLine + notificationsModel.NotificationDescription[i] + "," +
                        notificationsModel.SendDateTime[i] + "," +
                        notificationsModel.RecipientCount[i] + "," +
                        notificationsModel.LanguageType[i];
                    
                }
                System.IO.File.WriteAllText(path, csv);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public void ExportRecipients(string notificationDesc = "", string PNR = "", int NotificationID = 0, int extractID = 0)
        {
            log.Info("In EXPORT Recipients");
            RecipientsToCSV(notificationDesc, PNR, NotificationID, extractID);
        }

        public void RecipientsToCSV(string notificationDesc = "", string PNR = "", int NotificationID = 0, int extractID = 0)
        {
            string filename = "";
            if (NotificationID != 0)
            {
                //recipients for notification
                filename = "Recipients for Notifiation " + notificationDesc + ".csv";
            }
            else if (extractID != 0)
            {
                //recipients for a PNR
                filename = "Recipients for PNR " + PNR + ".csv";
            }
            else
            {
                filename = "Recent Recipients.csv";
            }
            string path = ConfigurationManager.AppSettings["ExportsPath"];
            path += filename;
            log.Debug("Exports File Directory: " + path);
            RecipientModel recipientModel = RecipientsReport(NotificationID, extractID, "", true);
            log.Debug("MODEL.RecipientID[0]: " + recipientModel.RecipientID[0]);
            //headers for the csv file
            string headers = "Sent To, Send Date Time, Send Type, Template Name";
            try
            {
                string csv = "";
                csv += headers;
                for (int i = 0; i < recipientModel.RecipientID.Count; i++)
                {
                    csv += Environment.NewLine + recipientModel.SentTo[i] + "," +
                    recipientModel.SendDateTime[i] + "," +
                    recipientModel.NotificationType[i] + "," +
                    recipientModel.TemplateName[i];

                }
                System.IO.File.WriteAllText(path, csv);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

        public void ExportExtracts(int recipientID = 0, string PNR = "", int dataSourceID = 0, string dataSourceName = "")
        {
            log.Info("In EXPORT Extracts");
            ExtractsToCSV(dataSourceName, PNR, dataSourceID, recipientID);
        }

        public void ExtractsToCSV(string dataSourceName = "", string PNR = "", int dataSourceID = 0, int recipientID = 0)
        {
            string filename = "";
            if (dataSourceID != 0)
            {
                //extracts for data source
                filename = "PNRs for Data Source " + dataSourceName + ".csv";
            }
            else if (recipientID != 0)
            {
                //extracts for recipient
                filename = "PNR " + PNR + ".csv";
            }
            else
            {
                filename = "Recent PNRs.csv";   
            }
            string path = ConfigurationManager.AppSettings["ExportsPath"];
            path += filename;
            log.Debug("Exports File Directory: " + path);
            ExtractsModel extractsModel = ExtractsReport(dataSourceID, recipientID, PNR, true);
            log.Debug("MODEL.extractID[0]: " + extractsModel.ExtractID[0]);
            //headers for the csv file
            string headers = "PNR, Lead Passenger Name, Upload Date Time, Booking Date, Email Address, Other Phone #, Language Code, Number of Flights";
            try
            {
                string csv = "";
                csv += headers;
                for (int i = 0; i < extractsModel.ExtractID.Count; i++)
                {
                    csv += Environment.NewLine + extractsModel.PNR[i] + "," +
                    extractsModel.LeadPaxName[i] + "," +
                    extractsModel.TotalPax[i] + "," +
                    extractsModel.UploadDateTime[i] + "," +
                    extractsModel.BookingDate[i] + "," +
                    extractsModel.EmailAddress[i] + "," +
                    extractsModel.OtherPhone[i] + "," +
                    extractsModel.LanguageCode[i] + "," +
                    extractsModel.NumberofFlights[i];
                }
                System.IO.File.WriteAllText(path, csv);
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }

    }
}
