using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.Entity.ModelConfiguration.Conventions;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System.IO;
using System.Data.SqlClient;
using Utils;
using System.Diagnostics;
using System.Threading;

namespace WeSupplyWorker
{
    public class IpConfig
    {
        public static string _remoteserver = "127.0.0.1";
        public static string _remoteport = "20001";
        public static string _localserver = "127.0.0.1";
        public static string _localport = "";
        public static string _remoteuser = "sa";
        public static string _remotepass = "Rte123456";
    }
    class Program
    {
        static void Main(string[] args)
        {
            Logger.init("WeSupplyWorker.log");
            String _machine = Environment.MachineName;
            Console.WriteLine(_machine);
            Console.WriteLine("WeSupplyWorker v. 12-11-2020-0929");
            // 12-11-2020-0929 Null check inside address 1 and 2 added
            // 12-01-2020-1046 Revision of Shipping address with ' in the text
            // 12-01-2020-1028 Updated the cancelled orders with the text Cancelled- at the beginning of the ssJSON text
            // 11-30-2020-1749 Removed restriction for quantity = 1 in automated
            // 11-25-2020-1053 Added EXEC to SP to Avoid automation of PO BOX and PR
            // 11-20-2020-1057 Avoid automation of PO BOX
            // 11-19-2020-1853 Added SSOrderID if not in Merlin yet
            // 11-19-2020-1822 Added try - catch to SS order to avoid error when the shipstation order do not exists yet
            // 11-19-2020-1318 The procedure changed to get all the items from yesterday only status 0 (pending) and 2 (confirmed)
            // 11-16-2020-0946 Added Call to SQL Procedure in order to popultate the non sent Pickings

            // Database opened for Procedure calls and Update the resulting AddressValidation Table rows
            dataAccess docInventst = new dataAccess("docInventst");
            SqlConnection conn0 = new SqlConnection();
            if (_machine == "SRV-ARM")
                conn0 = docInventst.OpenSqlDatabase(IpConfig._localserver + (IpConfig._localport != "" ? "," : "") + IpConfig._localport, "invictaHQ", "uid=" + IpConfig._remoteuser + ";pwd=" + IpConfig._remotepass + ";Trusted_Connection=no;Transaction Binding=Explicit Unbind;connection timeout=30");
            else
                conn0 = docInventst.OpenSqlDatabase(IpConfig._remoteserver + (IpConfig._remoteport != "" ? "," : "") + IpConfig._remoteport, "invictaHQ", "uid=" + IpConfig._remoteuser + ";pwd=" + IpConfig._remotepass);

            Logger.log("-- BEGIN PROCESSING --"); //kjdhfkjhdfkjh

            //// Get the pickings available in all the warehouses
            //String tmpSQL = "select ID,PickingLocation FROM [InvictaAUX].[dbo].[eCommercepicking] where isSent = 0 and PickingLocation IN('E0001', 'E-CONS01', 'EBAY001', 'OAK-L001', 'SHQ-L001', 'B0001')";
            //dataAccess eCPickings = new dataAccess("eCPickings");
            //DataSet dtseCPickings = new DataSet();
            //if (_machine == "SRV-ARM")
            //    dtseCPickings = eCPickings.QrySqlDatabase(IpConfig._localserver + (IpConfig._localport != "" ? "," : "") + IpConfig._localport, "invictaHQ", "uid=" + IpConfig._remoteuser + ";pwd=" + IpConfig._remotepass + ";Trusted_Connection=no;Transaction Binding=Explicit Unbind;connection timeout=30", tmpSQL, null);
            //else
            //    dtseCPickings = eCPickings.QrySqlDatabase(IpConfig._remoteserver + (IpConfig._remoteport != "" ? "," : "") + IpConfig._remoteport, "invictaHQ", "uid=" + IpConfig._remoteuser + ";pwd=" + IpConfig._remotepass, tmpSQL, null);
            //DataTable tbleCPickings = dtseCPickings.Tables["eCPickings"];
            //foreach (DataRow row in tbleCPickings.Rows)
            //{
                // Call the eCommerceOrderAddressvalidation Procedure to update the Address Validated table
                String tmpSQL = "EXEC InvictaAUX.[dbo].[eCommerceOrderAddressValidation] ;"; // @pickingID= " + row["ID"].ToString() + ", @Location= '" + row["PickingLocation"].ToString() + "'
            String _result = docInventst.nonQrySqlDatabase(conn0, tmpSQL, null, null); // Call the Procedure 
            Logger.log("Inserting Orders with eCommerceOrderAddressValidation, " + " result: " + (String.IsNullOrEmpty(_result) ? "OK" : _result)); //row["ID"].ToString() + " Location: " + row["PickingLocation"].ToString() + 
            //}

            //SELECT TOP 1 ID FROM InvictaAUX.dbo.eCommercePicking a WHERE a.isSent = 0 AND a.PickingLocation = @selLocation AND @selLocation<> '' ORDER BY a.[Time]
            // Returns all the NULL ssJSON in eCommerceOrderAddressValidated table     
            tmpSQL = "select OrderNo, max(ssOrderId) as ssOrderId, sum(quantity) as quantity from InvictaAUX.dbo.eCommerceOrderAddressValidated where ssJSON is NULL group by OrderNo"; //and ssOrderId is not NULL
            dataAccess eCOrders = new dataAccess("eCOrders");
            DataSet dtseCOrders = new DataSet();
            //_machine = "SRV-ARM"; // Only for test purpose
            if (_machine == "SRV-ARM")
                dtseCOrders = eCOrders.QrySqlDatabase(IpConfig._localserver + (IpConfig._localport != "" ? "," : "") + IpConfig._localport, "invictaHQ", "uid=" + IpConfig._remoteuser + ";pwd=" + IpConfig._remotepass + ";Trusted_Connection=no;Transaction Binding=Explicit Unbind;connection timeout=30", tmpSQL, null);
            else
                dtseCOrders = eCOrders.QrySqlDatabase(IpConfig._remoteserver + (IpConfig._remoteport != "" ? "," : "") + IpConfig._remoteport, "invictaHQ", "uid=" + IpConfig._remoteuser + ";pwd=" + IpConfig._remotepass, tmpSQL, null);
            DataTable tbleCOrders = dtseCOrders.Tables["eCOrders"];

            int _orders = 1;
            foreach (DataRow row in tbleCOrders.Rows)
            {
                String ssOrderID = row["ssOrderId"].ToString();
                String OrderNo = row["OrderNo"].ToString();
                if (OrderNo == "105609407")
                    Console.WriteLine(OrderNo);
                String OrderNo2 = OrderNo.Replace("EX", "");
                int Quantity = Convert.ToInt32(row["quantity"]);
                String URL = "https://ssapi.shipstation.com/orders?orderNumber=" + OrderNo2; // The Magento Order Number to find in Shipstation
                var client = new RestClient(URL);
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Host", "ssapi.shipstation.com");
                request.AddHeader("Authorization", "Basic MjJmZjM0MWMwMWFkNGE0ZWIxMDg5N2RiNzllMjUxMGI6ODNiYTg4ODFlMmE4NDM5MWE1MzU1Y2ZkNWJhOGRlNWM=");
                //request.AddParameter("orderNumber", "105561231");
                IRestResponse response = client.Execute(request);

                //Console.WriteLine(response.Content);
                String OrderStatus = "";
                try
                {
                    ShipStationOrder createdOrder = JsonConvert.DeserializeObject<ShipStationOrder>(response.Content);
                    ShipStationOrderRaul createdOrder2 = JsonConvert.DeserializeObject<ShipStationOrderRaul>(response.Content); // Optional to get the ssJSON ordered Raul's style
                    OrderStatus = createdOrder.Orders[0].OrderStatus;
                }
                catch (Exception _e)
                {
                    OrderStatus = "Not exists";
                    Logger.log("Order: " + OrderNo + " error in SS, result: " + _e.ToString());
                }

                if (OrderStatus == "shipped" || OrderStatus.IndexOf("shipment") >= 0) // If the order is viable then update table
                {
                    ShipStationOrder createdOrder = JsonConvert.DeserializeObject<ShipStationOrder>(response.Content);
                    String AddressVerified = createdOrder.Orders[0].ShipTo.AddressVerified;
                    int AddressValid = AddressVerified.IndexOf("Address validated successfully") >= 0 || AddressVerified.IndexOf("Address validation warning") >= 0 ? 1 : 0;
                    //String City = createdOrder.Orders[0].ShipTo.City;
                    //String Country = createdOrder.Orders[0].ShipTo.Country;
                    //String Phone = createdOrder.Orders[0].ShipTo.Phone;
                    //String PostalCode = createdOrder.Orders[0].ShipTo.PostalCode;
                    //String State = createdOrder.Orders[0].ShipTo.State;
                    //String ShippingAddress = createdOrder.Orders[0].ShipTo.Street1;
                    //String ShippingAddress2 = createdOrder.Orders[0].ShipTo.Street2;
                    String ssJSON = JsonConvert.SerializeObject(createdOrder); // populate ssJSON summary like Raul's with createdOrder2
                    ssJSON = ssJSON.Replace("'", "´"); // Avoid send ' to SQL tables
                    String ShippingAdress = String.IsNullOrEmpty(createdOrder.Orders[0].ShipTo.Street1) ? "" : createdOrder.Orders[0].ShipTo.Street1.Replace("'", "´");
                    String ShippingAdress2 = String.IsNullOrEmpty(createdOrder.Orders[0].ShipTo.Street2) ? "" : createdOrder.Orders[0].ShipTo.Street2.Replace("'", "´");
                    ssOrderID = (String.IsNullOrEmpty(ssOrderID) ? createdOrder.Orders[0].OrderId.ToString() : ssOrderID);
                    tmpSQL = "UPDATE InvictaAUX.dbo.eCommerceOrderAddressValidated	SET " +
                        //" ssOrderId = case when ssOrderId is NULL then " + (String.IsNullOrEmpty(ssOrderID) ? "NULL" : ssOrderID) + " else ssOrderId end" +
                        "ssOrderId = " + ssOrderID + 
                        ", City = '" + createdOrder.Orders[0].ShipTo.City + "'" +
                        ", Country = '" + createdOrder.Orders[0].ShipTo.Country + "'" +
                        ", Phone = '" + createdOrder.Orders[0].ShipTo.Phone + "'" +
                        ", postalCode = '" + createdOrder.Orders[0].ShipTo.PostalCode + "'" +
                        ", State = '" + createdOrder.Orders[0].ShipTo.State + "'" +
                        ", ShippingAdress = '" + ShippingAdress + "'" +
                        ", ShippingAdress2 = '" + ShippingAdress2 + "'" +
                        ", AddressValid = " + AddressValid.ToString() +
                        ", AutomatedOrder = " + (AddressValid == 1 ? 1 : 0).ToString() + // Quantity == 1 && 
                        ", ssJSON = '" + ssJSON + "'" +
                        " WHERE OrderNo='" + OrderNo + "'"; // and ssOrderId = " + ssOrderID; // (String.IsNullOrEmpty(ssOrderID) ? " is NULL" : "=" + ssOrderID);
                    _result = docInventst.nonQrySqlDatabase(conn0, tmpSQL, null, null);
                    Logger.log("Updated Order: " + OrderNo + " ssOrderId: " + ssOrderID + " result: " + (String.IsNullOrEmpty(_result) ? "OK": _result));
                    Console.WriteLine(_orders.ToString() + "- Updated Order: " + OrderNo + " ssOrderId: " + ssOrderID + " result: " + (String.IsNullOrEmpty(_result) ? "OK" : _result));

                    // Removing automated flag for PO BOX and Puerto Rico
                    tmpSQL = "EXEC InvictaAUX.[dbo].[eCommerceOrderAddressValidationOFF] ; ";
                    _result = docInventst.nonQrySqlDatabase(conn0, tmpSQL, null, null);
                }
                else if (OrderStatus == "cancelled")
                {
                    ShipStationOrder createdOrder = JsonConvert.DeserializeObject<ShipStationOrder>(response.Content);
                    //tmpSQL = "delete from InvictaAUX.dbo.eCommerceOrderAddressValidated WHERE OrderNo='" + OrderNo + "'; ";
                    String ssJSON = JsonConvert.SerializeObject(createdOrder); // populate ssJSON summary like Raul's with createdOrder2
                    ssJSON = ssJSON.Replace("'", "´"); // Avoid send ' to SQL tables
                    tmpSQL = "update InvictaAUX.dbo.eCommerceOrderAddressValidated set ssJSON= 'Cancelled - " + ssJSON + "' WHERE OrderNo='" + OrderNo + "'; ";
                    _result = docInventst.nonQrySqlDatabase(conn0, tmpSQL, null, null);
                    Logger.log("Updating cancelled Order: " + OrderNo + " ssOrderId: " + ssOrderID + " result: " + (String.IsNullOrEmpty(_result) ? "OK" : _result));
                    Console.WriteLine(_orders.ToString() + "- Updating cancelled Order: " + OrderNo + " ssOrderId: " + ssOrderID + " result: " + (String.IsNullOrEmpty(_result) ? "OK" : _result));

                }
                _orders++;
                if (_orders>40) // Stop for a minute when reach 40 orders processed
                {
                    //Console.WriteLine("Waiting a minute ....");
                    //Thread.Sleep(60000); // wait one second before continuing
                    //_orders = 1;
                }
            }
            conn0.Close();
            Logger.log("-- END PROCESSING --");
        }
    }
}
