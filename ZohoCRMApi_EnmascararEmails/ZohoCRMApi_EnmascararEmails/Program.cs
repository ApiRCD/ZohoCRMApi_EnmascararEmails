// See https://aka.ms/new-console-template for more information
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.Net;
using System.Resources;
using System.Text.RegularExpressions;
using ZohoCRMApi_EnmascararEmails.Properties;


Console.WriteLine("Enmascaramiento de Emails de ZohoCRM iniciando ...");

// Carpeta donde se guardan los logs
string path = Resources.File_Local_Logs;
string AccessToken = "";
string URL = "";
int Unmasking_Avails = 1;


// Crear la carpeta si no existe
if (!Directory.Exists(path))
{
    Directory.CreateDirectory(path);
    Console.WriteLine("Carpeta creada "+ path + " .");
}

// Es el mismo link para prod y sandbox de Zoho para el refresh token.
if (Resources.Prod == "1") {

    // Prod
    URL = "https://accounts.zoho.com/oauth/v2/token?refresh_token=" + Resources.refresh_token_LD + "&grant_type=refresh_token&client_id=" + Resources.client_id_LD + "&client_secret=" + Resources.client_secret_LD + "&redirect_uri=" + Resources.redirect_uri_LD + "&scope=" + Resources.Scope + "";

} else { 

    // QA
    URL = "https://accounts.zoho.com/oauth/v2/token?refresh_token=" + Resources.refresh_token_QA + "&grant_type=refresh_token&client_id=" + Resources.client_id_QA + "&client_secret=" + Resources.client_secret_QA + "&redirect_uri=" + Resources.redirect_uri_QA + "&scope=" + Resources.Scope + "";

}


// Ejecutamos el Do while.
Console.WriteLine("Se ejecuto el Do While.");
File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Se ejecuto el Do While.");

// Do While para recorrer todos los leads de zoho.
do
{


    // ************ Refresh Token ************ //


    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
    request.Method = "POST";
    request.Headers.Add("cache-control", "no-cache");
    request.Headers.Add("cookie", "stk=fc10d77d428bbfa8fd2edf9e94bd713c; dcl_pfx_lcnt=0; a6c51d9354=57c7a14afabcac9a0b9dfc64b3542b70; iamcsr=48b6fc5d-3e62-4609-9829-7ba4cf0434a9; JSESSIONID=909571B74C55BCD65BB8E00CE1C0908E; a8c61fa0dc=412d04ceb86ecaf57aa7a1d4903c681d");
    request.Headers.Add("Postman-Token", "45a6476e-62ef-479a-b508-b906772d14a2,0d7c56fb-4eea-4117-bbe2-ef9787e26a57");

    try
    {
        WebResponse webResponse = request.GetResponse();
        Stream webStream = webResponse.GetResponseStream();
        StreamReader responseReader = new StreamReader(webStream);
        string response = responseReader.ReadToEnd();
        dynamic data = JObject.Parse(response);

        AccessToken = data.access_token;

        responseReader.Close();

        // Escribimos en el archivo el Access Token
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Se refresco el token.");
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + AccessToken);

        // Imprimimos el Access Token
        Console.WriteLine("Access Token : " + AccessToken);

    }
    catch (Exception e)
    {
        Console.Out.WriteLine(e.Message);
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Error en el Token : " + e);

        if (e.Message == "The remote server returned an error: (401) .")
        {
            Console.Out.WriteLine("Se detuvo el software por 1 minuto por error 401 del token de Zoho.");
            File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + "Se detuvo el software por 1 minuto por error 401 del token de Zoho.");

            Thread.Sleep(60000);

        }


    }





    // ************ SQL ************ //

    if (Resources.Prod == "1")
    {
        // Prod
        URL = "https://www.zohoapis.com/crm/v2/coql";

    } else {

        // QA
        URL = "https://crmsandbox.zoho.com/crm/v2/coql";

    }

    var requestSQL = (HttpWebRequest)WebRequest.Create(URL);
    requestSQL.Method = "POST";

    requestSQL.Headers.Add("cache-control", "no-cache");
    requestSQL.Headers.Add("accept-encoding", "deflate");
    requestSQL.Headers.Add("cookie", "stk=fc10d77d428bbfa8fd2edf9e94bd713c; dcl_pfx_lcnt=0; c026b7d48a=00ad0429837d682dadcd1fa2d808aa64; JSESSIONID=E25C8DED610CE0FA1E735B37F17135D8; crmcsr=688e39c1-ccde-492a-a73b-7fe177ca2e1e");
    requestSQL.Headers.Add("Postman-Token", "fb8ee0fd-64cc-4270-9e52-501fbd3aee6d,4cce079a-212b-4be6-870c-0ed639e372bc");
    requestSQL.Headers.Add("Cache-Control", "no-cache");
    requestSQL.Headers.Add("Authorization", "Zoho-oauthtoken " + AccessToken);



    //Email not like '%relay.rcdservices.io%'
    using (var streamWriter = new StreamWriter(requestSQL.GetRequestStream()))
    {
        string json = "";

        // Query
        json = "{\"select_query\" : \"select id,First_Name,Last_Name,Email from Contacts where  Email not like '%relay.rcdservices.io%' and Email is not null  limit 100\"}";
        //id = 2482482000363732047 and

        streamWriter.Write(json);
    }




    try
    {


        WebResponse webResponse = requestSQL.GetResponse();
        Stream webStream = webResponse.GetResponseStream();
        StreamReader responseReader = new StreamReader(webStream);
        string response = responseReader.ReadToEnd();

        // Imprimimos el resultado de SQL
        Console.WriteLine("SQL response Result : ");
        Console.WriteLine(response);

        // Si existen emails no enmascarados disponibles.
        if (response == null || response == "")
        {


            // Imprimimos si ya no existen mas emails enmascarados.
            Console.WriteLine("Ya no existen emails no enmascarados.");
            File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Ya no existen emails no enmascarados.");

            // Cambiamos el valor del Unmasking_Avails para terminar el While.
            Unmasking_Avails = 0;
            //break;

        }
        else
        {

            dynamic data = JObject.Parse(response);


            // Imprimimos el resultado de SQL parseado
            //Console.WriteLine("SQL Result : ");
            //Console.WriteLine(data);

            // Escribimos en el archivo el resultado de la consulta SQL
            File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Se ejecuto ZohoCRM SQL.");
            File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + data);


            // Recorrer el JObject con foreach
            foreach (var item in data["data"])
            {


                // Imprimimos ID y Email en consola
                Console.WriteLine($"id: {item["id"]}, Email: {item["Email"]}");
                File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + $"id: {item["id"]}, Email: {item["Email"]}");


                String ID = item["id"];
                String Email = item["Email"];


                // ************ Mask Email ************ //

                if (Resources.Prod == "1")
                {

                    // Prod
                    URL = "https://integrations.rcdservices.io/Middleware/api/ApiMasking/MaskEmail";

                }
                else
                {

                    // QA
                    URL = "https://iis-qas.rcdservices.io/Middleware/api/ApiMasking/MaskEmail";

                }

                var requestMaskingEmail = (HttpWebRequest)WebRequest.Create(URL);
                requestMaskingEmail.Method = "POST";

                requestMaskingEmail.Headers.Add("Content-Type", "application/json");
                requestMaskingEmail.Headers.Add("cache-control", "no-cache");
                requestMaskingEmail.Headers.Add("accept-encoding", "deflate");
                requestMaskingEmail.Headers.Add("cookie", "stk=fc10d77d428bbfa8fd2edf9e94bd713c; dcl_pfx_lcnt=0; c026b7d48a=00ad0429837d682dadcd1fa2d808aa64; JSESSIONID=E25C8DED610CE0FA1E735B37F17135D8; crmcsr=688e39c1-ccde-492a-a73b-7fe177ca2e1e");
                requestMaskingEmail.Headers.Add("Postman-Token", "fb8ee0fd-64cc-4270-9e52-501fbd3aee6d,4cce079a-212b-4be6-870c-0ed639e372bc");
                requestMaskingEmail.Headers.Add("Cache-Control", "no-cache");
                //requestMaskingEmail.Headers.Add("Authorization", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJwcm9ncmFtYWNpb25BUEkiLCJOYW1lIjoiUHJvZ3JhbWFjaW9uIEFQSSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6ImFkbWluIiwianRpIjoiYzIxMzJhODQtY2U5Ny00ZGViLWE4NjItNjMyYTcxZjA2ZjY3IiwiQVBJTnV2b2xhIjoiQVBJTnV2b2xhIiwiQVBJUm9vbVNlcnZpY2UiOiJBUElSb29tU2VydmljZSIsIkFQSUFkbWluUkNEIjoiQVBJQWRtaW5SQ0QiLCJBUElDb2Jyb3NSQ0QiOiJBUElDb2Jyb3NSQ0QiLCJBUElHZW5lcmljb3MiOiJBUElHZW5lcmljb3MiLCJBUElQb29sU2VydmljZSI6IkFQSVBvb2xTZXJ2aWNlIiwiQVBJU1NGRiI6IkFQSVNTRkYiLCJBUElFbWFpbCI6IkFQSUVtYWlsIiwiQVBJT1dTIjoiQVBJT1dTIiwiQVBJTWFza2luZyI6IkFQSU1hc2tpbmciLCJleHAiOjE3MTUwOTc4MTUsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjgwODEvIiwiYXVkIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6ODA4MS8ifQ.UxlOxtirIFPkaAaGIlF3_mpQAB2km2pf_h8SpiNjiN8");

                using (var streamWriter = new StreamWriter(requestMaskingEmail.GetRequestStream()))
                {

                    string json = "";


                    json = "{\r\n    \"email\": \"" + Email + "\" ,\r\n    \"id\": \"" + ID + "\",\r\n    \"sistema\": \"Zoho\"\r\n}";


                    streamWriter.Write(json);

                }


                try
                {


                    WebResponse webResponse_MaskEmail = requestMaskingEmail.GetResponse();
                    Stream webStream_MaskEmail = webResponse_MaskEmail.GetResponseStream();
                    StreamReader responseReader_MaskEmail = new StreamReader(webStream_MaskEmail);
                    string response_MaskEmail = responseReader_MaskEmail.ReadToEnd();
                    dynamic data_MaskEmail = JObject.Parse(response_MaskEmail);



                    // Imprimimos el resultado del Api Masking
                    Console.WriteLine("Api Masking Result : ");
                    Console.WriteLine(data_MaskEmail);

                    // Escribimos en el archivo el resultado del Api Masking
                    File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Api Masking Result : ");
                    File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + data_MaskEmail);



                    // ************ Update Zoho ************ //

                    if (Resources.Prod == "1")
                    {

                        // Prod
                        URL = "https://www.zohoapis.com/crm/v2/Contacts/" + ID;

                    }
                    else
                    {

                        // QA
                        URL = "https://crmsandbox.zoho.com/crm/v2/Contacts/" + ID;

                    }

                    var request_PutMaskingEmail = (HttpWebRequest)WebRequest.Create(URL);
                    request_PutMaskingEmail.Method = "PUT";

                    request_PutMaskingEmail.Headers.Add("Content-Type", "application/json");
                    request_PutMaskingEmail.Headers.Add("cache-control", "no-cache");
                    request_PutMaskingEmail.Headers.Add("accept-encoding", "deflate");
                    request_PutMaskingEmail.Headers.Add("cookie", "stk=fc10d77d428bbfa8fd2edf9e94bd713c; dcl_pfx_lcnt=0; c026b7d48a=00ad0429837d682dadcd1fa2d808aa64; JSESSIONID=E25C8DED610CE0FA1E735B37F17135D8; crmcsr=688e39c1-ccde-492a-a73b-7fe177ca2e1e");
                    request_PutMaskingEmail.Headers.Add("Postman-Token", "fb8ee0fd-64cc-4270-9e52-501fbd3aee6d,4cce079a-212b-4be6-870c-0ed639e372bc");
                    request_PutMaskingEmail.Headers.Add("Cache-Control", "no-cache");
                    request_PutMaskingEmail.Headers.Add("Authorization", "Zoho-oauthtoken " + AccessToken);
                    request_PutMaskingEmail.Headers.Add("scope", "ZohoCRM.modules.custom.all,ZohoCRM.modules.contacts.all,ZohoCRM.modules.accounts.all,ZohoCRM.modules.deals.all,ZohoCRM.modules.events.all,ZohoCRM.modules.tasks.all,ZohoCRM.modules.calls.all,ZohoCRM.modules.invoices.all,ZohoCRM.modules.pricebooks.all,ZohoCRM.modules.salesorders.all,ZohoCRM.modules.purchaseorders.all,ZohoCRM.modules.products.all,ZohoCRM.modules.cases.all,ZohoCRM.modules.solutions.all,ZohoCRM.modules.vendors.all,ZohoCRM.modules.quotes.all,ZohoCRM.modules.ALL,ZohoCRM.settings.ALL,ZohoCRM.users.ALL,ZohoCRM.org.ALL,aaaserver.profile.ALL,ZohoCRM.settings.functions.all,ZohoCRM.functions.execute.read,ZohoCRM.functions.execute.create,ZohoCRM.settings.layout_rules.read,ZohoCRM.notifications.all,ZohoCRM.coql.READ");

                    using (var streamWriter = new StreamWriter(request_PutMaskingEmail.GetRequestStream()))
                    {

                        string json = "";

                        // Sensible a mayucuslas y minusculas en los campos de Zoho
                        json = "{\r\n  \"data\": [\r\n    {\r\n      \"Email\": \"" + data_MaskEmail["maskedEmail"] + "\" \r\n    }\r\n  ]\r\n}";


                        streamWriter.Write(json);

                    }

                    WebResponse webResponse_PutMaskingEmail = request_PutMaskingEmail.GetResponse();
                    Stream webStream_PutMaskingEmail = webResponse_PutMaskingEmail.GetResponseStream();
                    StreamReader responseReader_PutMaskingEmail = new StreamReader(webStream_PutMaskingEmail);
                    string response_PutMaskingEmail = responseReader_PutMaskingEmail.ReadToEnd();
                    dynamic data_PutMaskingEmail = JObject.Parse(response_PutMaskingEmail);


                    // Imprimimos el resultado del Update de Zoho
                    //Console.WriteLine("Token - Update Zoho Result Email: ");
                    //Console.WriteLine(data_PutMaskingEmail);


                    // Escribimos en el archivo el resultado del Update de Zoho
                    //File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Se ejecuto el Update de Zoho CRM.");
                    //File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + data_PutMaskingEmail);



                    // ************ Enmascaramos el email ************ //


                    string pattern = @"(?<=[\w]{1})[\w\-._\+%]*(?=[\w]{1}@)";
                    string result_Email = Regex.Replace(Email, pattern, m => new string('*', m.Length));


                    // ************ Update Zoho Email Masked - Email_Address  ************ //

                    if (Resources.Prod == "1")
                    {

                        // Prod
                        URL = "https://www.zohoapis.com/crm/v2/Contacts/" + ID;

                    }
                    else
                    {

                        // QA
                        URL = "https://crmsandbox.zoho.com/crm/v2/Contacts/" + ID;

                    }

                    var request_PutMaskingEmailAddress = (HttpWebRequest)WebRequest.Create(URL);
                    request_PutMaskingEmailAddress.Method = "PUT";

                    request_PutMaskingEmailAddress.Headers.Add("Content-Type", "application/json");
                    request_PutMaskingEmailAddress.Headers.Add("cache-control", "no-cache");
                    request_PutMaskingEmailAddress.Headers.Add("accept-encoding", "deflate");
                    request_PutMaskingEmailAddress.Headers.Add("cookie", "stk=fc10d77d428bbfa8fd2edf9e94bd713c; dcl_pfx_lcnt=0; c026b7d48a=00ad0429837d682dadcd1fa2d808aa64; JSESSIONID=E25C8DED610CE0FA1E735B37F17135D8; crmcsr=688e39c1-ccde-492a-a73b-7fe177ca2e1e");
                    request_PutMaskingEmailAddress.Headers.Add("Postman-Token", "fb8ee0fd-64cc-4270-9e52-501fbd3aee6d,4cce079a-212b-4be6-870c-0ed639e372bc");
                    request_PutMaskingEmailAddress.Headers.Add("Cache-Control", "no-cache");
                    request_PutMaskingEmailAddress.Headers.Add("Authorization", "Zoho-oauthtoken " + AccessToken);
                    request_PutMaskingEmailAddress.Headers.Add("scope", "ZohoCRM.modules.custom.all,ZohoCRM.modules.contacts.all,ZohoCRM.modules.accounts.all,ZohoCRM.modules.deals.all,ZohoCRM.modules.events.all,ZohoCRM.modules.tasks.all,ZohoCRM.modules.calls.all,ZohoCRM.modules.invoices.all,ZohoCRM.modules.pricebooks.all,ZohoCRM.modules.salesorders.all,ZohoCRM.modules.purchaseorders.all,ZohoCRM.modules.products.all,ZohoCRM.modules.cases.all,ZohoCRM.modules.solutions.all,ZohoCRM.modules.vendors.all,ZohoCRM.modules.quotes.all,ZohoCRM.modules.ALL,ZohoCRM.settings.ALL,ZohoCRM.users.ALL,ZohoCRM.org.ALL,aaaserver.profile.ALL,ZohoCRM.settings.functions.all,ZohoCRM.functions.execute.read,ZohoCRM.functions.execute.create,ZohoCRM.settings.layout_rules.read,ZohoCRM.notifications.all,ZohoCRM.coql.READ");

                    using (var streamWriter = new StreamWriter(request_PutMaskingEmailAddress.GetRequestStream()))
                    {

                        string json = "";

                        // Sensible a mayucuslas y minusculas en los campos de Zoho
                        json = "{\r\n  \"data\": [\r\n    {\r\n      \"Email_Address\": \" " + result_Email + " \" \r\n    }\r\n  ]\r\n}";


                        streamWriter.Write(json);

                    }

                    WebResponse webResponse_PutMaskingEmailAddress = request_PutMaskingEmailAddress.GetResponse();
                    Stream webStream_PutMaskingEmailAddress = webResponse_PutMaskingEmailAddress.GetResponseStream();
                    StreamReader responseReader_PutMaskingEmailAddress = new StreamReader(webStream_PutMaskingEmailAddress);
                    string response_PutMaskingEmailAddress = responseReader_PutMaskingEmailAddress.ReadToEnd();
                    dynamic data_PutMaskingEmailAddress = JObject.Parse(response_PutMaskingEmailAddress);


                    // Imprimimos el resultado del Update de Zoho
                    //Console.WriteLine("Mask - Update Zoho Result Email_Address: ");
                    //Console.WriteLine(data_PutMaskingEmailAddress);


                    // Escribimos en el archivo el resultado del Update de Zoho
                    //File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Se ejecuto el Update de Zoho CRM.");
                    //File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + data_PutMaskingEmailAddress);



                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                    File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Error MaskingAPI: " + e);

                }



            }


        }

        // Imprimimos el resultado del Update de Zoho
        Console.WriteLine("");
        Console.WriteLine("######################################################");
        Console.WriteLine("Se termino de ejecutar el enmascaramiento de emails.");
        Console.WriteLine("######################################################");

        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " ######################################################.");
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Se termino de ejecutar el enmascaramiento de emails.");
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " ######################################################.");

    }
    catch (Exception e)
    {

        Console.Out.WriteLine(e.Message);
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Error en el SQL: " + e);

    }












} while (Unmasking_Avails == 1 && Resources.Mask_All == "1");




// ****************************************** //
// ************ Email Secundario ************ //
// ****************************************** //


do
{


    // ************ Refresh Token ************ //


    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(URL);
    request.Method = "POST";
    request.Headers.Add("cache-control", "no-cache");
    request.Headers.Add("cookie", "stk=fc10d77d428bbfa8fd2edf9e94bd713c; dcl_pfx_lcnt=0; a6c51d9354=57c7a14afabcac9a0b9dfc64b3542b70; iamcsr=48b6fc5d-3e62-4609-9829-7ba4cf0434a9; JSESSIONID=909571B74C55BCD65BB8E00CE1C0908E; a8c61fa0dc=412d04ceb86ecaf57aa7a1d4903c681d");
    request.Headers.Add("Postman-Token", "45a6476e-62ef-479a-b508-b906772d14a2,0d7c56fb-4eea-4117-bbe2-ef9787e26a57");

    try
    {
        WebResponse webResponse = request.GetResponse();
        Stream webStream = webResponse.GetResponseStream();
        StreamReader responseReader = new StreamReader(webStream);
        string response = responseReader.ReadToEnd();
        dynamic data = JObject.Parse(response);

        AccessToken = data.access_token;

        responseReader.Close();

        // Escribimos en el archivo el Access Token
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Se refresco el token.");
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + AccessToken);

        // Imprimimos el Access Token
        Console.WriteLine("Access Token : " + AccessToken);

    }
    catch (Exception e)
    {
        Console.Out.WriteLine(e.Message);
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Error en el Token : " + e);

        if (e.Message == "The remote server returned an error: (401) .")
        {
            Console.Out.WriteLine("Se detuvo el software por 1 minuto por error 401 del token de Zoho.");
            File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + "Se detuvo el software por 1 minuto por error 401 del token de Zoho.");

            Thread.Sleep(60000);

        }

    }





    // ************ SQL ************ //

    if (Resources.Prod == "1")
    {
        // Prod
        URL = "https://www.zohoapis.com/crm/v2/coql";

    }
    else
    {

        // QA
        URL = "https://crmsandbox.zoho.com/crm/v2/coql";

    }

    var requestSQL = (HttpWebRequest)WebRequest.Create(URL);
    requestSQL.Method = "POST";

    requestSQL.Headers.Add("cache-control", "no-cache");
    requestSQL.Headers.Add("accept-encoding", "deflate");
    requestSQL.Headers.Add("cookie", "stk=fc10d77d428bbfa8fd2edf9e94bd713c; dcl_pfx_lcnt=0; c026b7d48a=00ad0429837d682dadcd1fa2d808aa64; JSESSIONID=E25C8DED610CE0FA1E735B37F17135D8; crmcsr=688e39c1-ccde-492a-a73b-7fe177ca2e1e");
    requestSQL.Headers.Add("Postman-Token", "fb8ee0fd-64cc-4270-9e52-501fbd3aee6d,4cce079a-212b-4be6-870c-0ed639e372bc");
    requestSQL.Headers.Add("Cache-Control", "no-cache");
    requestSQL.Headers.Add("Authorization", "Zoho-oauthtoken " + AccessToken);




    using (var streamWriter = new StreamWriter(requestSQL.GetRequestStream()))
    {
        string json = "";

        // Query
        json = "{\"select_query\" : \"select id,First_Name,Last_Name,Secondary_Email from Contacts where  Secondary_Email not like '%relay.rcdservices.io%' and Secondary_Email is not null limit 100\"}";
        //where Secondary_Email not like '%relay.rcdservices.io%'
        //id = 2482482000363732047 and
        streamWriter.Write(json);
    }

    


    try
    {


        WebResponse webResponse = requestSQL.GetResponse();
        Stream webStream = webResponse.GetResponseStream();
        StreamReader responseReader = new StreamReader(webStream);
        string response = responseReader.ReadToEnd();

        // Imprimimos el resultado de SQL
        Console.WriteLine("SQL response Result : ");
        Console.WriteLine(response);

        // Si existen emails no enmascarados disponibles.
        if (response == null || response == "")
        {


            // Imprimimos si ya no existen mas emails enmascarados.
            Console.WriteLine("Ya no existen Secondary Emails no enmascarados.");
            File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Ya no existen Secondary Emails no enmascarados.");

            // Cambiamos el valor del Unmasking_Avails para terminar el While.
            Unmasking_Avails = 0;
            //break;

        }
        else
        {

            dynamic data = JObject.Parse(response);


            // Imprimimos el resultado de SQL parseado
            //Console.WriteLine("SQL Result : ");
            //Console.WriteLine(data);

            // Escribimos en el archivo el resultado de la consulta SQL
            File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Se ejecuto ZohoCRM SQL.");
            File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + data);


            // Recorrer el JObject con foreach
            foreach (var item in data["data"])
            {


                // Imprimimos ID y Email en consola
                Console.WriteLine($"id: {item["id"]}, Secondary_Email: {item["Secondary_Email"]}");
                File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + $"id: {item["id"]}, Secondary_Email: {item["Secondary_Email"]}");

                String ID = item["id"];
                String Email = item["Secondary_Email"];


                // ************ Mask Email ************ //

                if (Resources.Prod == "1")
                {

                    // Prod
                    URL = "https://integrations.rcdservices.io/Middleware/api/ApiMasking/MaskEmail";

                }
                else
                {

                    // QA
                    URL = "https://iis-qas.rcdservices.io/Middleware/api/ApiMasking/MaskEmail";

                }

                var requestMaskingEmail = (HttpWebRequest)WebRequest.Create(URL);
                requestMaskingEmail.Method = "POST";

                requestMaskingEmail.Headers.Add("Content-Type", "application/json");
                requestMaskingEmail.Headers.Add("cache-control", "no-cache");
                requestMaskingEmail.Headers.Add("accept-encoding", "deflate");
                requestMaskingEmail.Headers.Add("cookie", "stk=fc10d77d428bbfa8fd2edf9e94bd713c; dcl_pfx_lcnt=0; c026b7d48a=00ad0429837d682dadcd1fa2d808aa64; JSESSIONID=E25C8DED610CE0FA1E735B37F17135D8; crmcsr=688e39c1-ccde-492a-a73b-7fe177ca2e1e");
                requestMaskingEmail.Headers.Add("Postman-Token", "fb8ee0fd-64cc-4270-9e52-501fbd3aee6d,4cce079a-212b-4be6-870c-0ed639e372bc");
                requestMaskingEmail.Headers.Add("Cache-Control", "no-cache");
                //requestMaskingEmail.Headers.Add("Authorization", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJwcm9ncmFtYWNpb25BUEkiLCJOYW1lIjoiUHJvZ3JhbWFjaW9uIEFQSSIsImh0dHA6Ly9zY2hlbWFzLm1pY3Jvc29mdC5jb20vd3MvMjAwOC8wNi9pZGVudGl0eS9jbGFpbXMvcm9sZSI6ImFkbWluIiwianRpIjoiYzIxMzJhODQtY2U5Ny00ZGViLWE4NjItNjMyYTcxZjA2ZjY3IiwiQVBJTnV2b2xhIjoiQVBJTnV2b2xhIiwiQVBJUm9vbVNlcnZpY2UiOiJBUElSb29tU2VydmljZSIsIkFQSUFkbWluUkNEIjoiQVBJQWRtaW5SQ0QiLCJBUElDb2Jyb3NSQ0QiOiJBUElDb2Jyb3NSQ0QiLCJBUElHZW5lcmljb3MiOiJBUElHZW5lcmljb3MiLCJBUElQb29sU2VydmljZSI6IkFQSVBvb2xTZXJ2aWNlIiwiQVBJU1NGRiI6IkFQSVNTRkYiLCJBUElFbWFpbCI6IkFQSUVtYWlsIiwiQVBJT1dTIjoiQVBJT1dTIiwiQVBJTWFza2luZyI6IkFQSU1hc2tpbmciLCJleHAiOjE3MTUwOTc4MTUsImlzcyI6Imh0dHBzOi8vbG9jYWxob3N0OjgwODEvIiwiYXVkIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6ODA4MS8ifQ.UxlOxtirIFPkaAaGIlF3_mpQAB2km2pf_h8SpiNjiN8");

                using (var streamWriter = new StreamWriter(requestMaskingEmail.GetRequestStream()))
                {

                    string json = "";


                    json = "{\r\n    \"email\": \"" + Email + "\" ,\r\n    \"id\": \"" + ID + "\",\r\n    \"sistema\": \"Zoho\"\r\n}";


                    streamWriter.Write(json);

                }


                try
                {


                    WebResponse webResponse_MaskEmail = requestMaskingEmail.GetResponse();
                    Stream webStream_MaskEmail = webResponse_MaskEmail.GetResponseStream();
                    StreamReader responseReader_MaskEmail = new StreamReader(webStream_MaskEmail);
                    string response_MaskEmail = responseReader_MaskEmail.ReadToEnd();
                    dynamic data_MaskEmail = JObject.Parse(response_MaskEmail);



                    // Imprimimos el resultado del Api Masking
                    Console.WriteLine("Api Masking Result : ");
                    Console.WriteLine(data_MaskEmail);

                    // Escribimos en el archivo el resultado del Api Masking
                    File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Api Masking Result : ");
                    File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + data_MaskEmail);



                    // ************ Update Zoho ************ //

                    if (Resources.Prod == "1")
                    {

                        // Prod
                        URL = "https://www.zohoapis.com/crm/v2/Contacts/" + ID;

                    }
                    else
                    {

                        // QA
                        URL = "https://crmsandbox.zoho.com/crm/v2/Contacts/" + ID;

                    }

                    var request_PutMaskingEmail = (HttpWebRequest)WebRequest.Create(URL);
                    request_PutMaskingEmail.Method = "PUT";

                    request_PutMaskingEmail.Headers.Add("Content-Type", "application/json");
                    request_PutMaskingEmail.Headers.Add("cache-control", "no-cache");
                    request_PutMaskingEmail.Headers.Add("accept-encoding", "deflate");
                    request_PutMaskingEmail.Headers.Add("cookie", "stk=fc10d77d428bbfa8fd2edf9e94bd713c; dcl_pfx_lcnt=0; c026b7d48a=00ad0429837d682dadcd1fa2d808aa64; JSESSIONID=E25C8DED610CE0FA1E735B37F17135D8; crmcsr=688e39c1-ccde-492a-a73b-7fe177ca2e1e");
                    request_PutMaskingEmail.Headers.Add("Postman-Token", "fb8ee0fd-64cc-4270-9e52-501fbd3aee6d,4cce079a-212b-4be6-870c-0ed639e372bc");
                    request_PutMaskingEmail.Headers.Add("Cache-Control", "no-cache");
                    request_PutMaskingEmail.Headers.Add("Authorization", "Zoho-oauthtoken " + AccessToken);
                    request_PutMaskingEmail.Headers.Add("scope", "ZohoCRM.modules.custom.all,ZohoCRM.modules.contacts.all,ZohoCRM.modules.accounts.all,ZohoCRM.modules.deals.all,ZohoCRM.modules.events.all,ZohoCRM.modules.tasks.all,ZohoCRM.modules.calls.all,ZohoCRM.modules.invoices.all,ZohoCRM.modules.pricebooks.all,ZohoCRM.modules.salesorders.all,ZohoCRM.modules.purchaseorders.all,ZohoCRM.modules.products.all,ZohoCRM.modules.cases.all,ZohoCRM.modules.solutions.all,ZohoCRM.modules.vendors.all,ZohoCRM.modules.quotes.all,ZohoCRM.modules.ALL,ZohoCRM.settings.ALL,ZohoCRM.users.ALL,ZohoCRM.org.ALL,aaaserver.profile.ALL,ZohoCRM.settings.functions.all,ZohoCRM.functions.execute.read,ZohoCRM.functions.execute.create,ZohoCRM.settings.layout_rules.read,ZohoCRM.notifications.all,ZohoCRM.coql.READ");

                    using (var streamWriter = new StreamWriter(request_PutMaskingEmail.GetRequestStream()))
                    {

                        string json = "";

                        // Sensible a mayucuslas y minusculas en los campos de Zoho
                        json = "{\r\n  \"data\": [\r\n    {\r\n      \"Secondary_Email\": \" " + data_MaskEmail["maskedEmail"] + " \" \r\n    }\r\n  ]\r\n}";


                        streamWriter.Write(json);

                    }

                    WebResponse webResponse_PutMaskingEmail = request_PutMaskingEmail.GetResponse();
                    Stream webStream_PutMaskingEmail = webResponse_PutMaskingEmail.GetResponseStream();
                    StreamReader responseReader_PutMaskingEmail = new StreamReader(webStream_PutMaskingEmail);
                    string response_PutMaskingEmail = responseReader_PutMaskingEmail.ReadToEnd();
                    dynamic data_PutMaskingEmail = JObject.Parse(response_PutMaskingEmail);


                    // Imprimimos el resultado del Update de Zoho
                    //Console.WriteLine("Token - Update Zoho Result Secondary_Email: ");
                    //Console.WriteLine(data_PutMaskingEmail);


                    // Escribimos en el archivo el resultado del Update de Zoho
                    //File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Se ejecuto el Update de Zoho CRM.");
                    //File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + data_PutMaskingEmail);


                    // ************ Enmascaramos el email ************ //


                    string pattern = @"(?<=[\w]{1})[\w\-._\+%]*(?=[\w]{1}@)";
                    string result_Email = Regex.Replace(Email, pattern, m => new string('*', m.Length));


                    // ************ Update Zoho Email Masked - Email_Address  ************ //

                    if (Resources.Prod == "1")
                    {

                        // Prod
                        URL = "https://www.zohoapis.com/crm/v2/Contacts/" + ID;

                    }
                    else
                    {

                        // QA
                        URL = "https://crmsandbox.zoho.com/crm/v2/Contacts/" + ID;

                    }

                    var request_PutMaskingEmailAddress = (HttpWebRequest)WebRequest.Create(URL);
                    request_PutMaskingEmailAddress.Method = "PUT";

                    request_PutMaskingEmailAddress.Headers.Add("Content-Type", "application/json");
                    request_PutMaskingEmailAddress.Headers.Add("cache-control", "no-cache");
                    request_PutMaskingEmailAddress.Headers.Add("accept-encoding", "deflate");
                    request_PutMaskingEmailAddress.Headers.Add("cookie", "stk=fc10d77d428bbfa8fd2edf9e94bd713c; dcl_pfx_lcnt=0; c026b7d48a=00ad0429837d682dadcd1fa2d808aa64; JSESSIONID=E25C8DED610CE0FA1E735B37F17135D8; crmcsr=688e39c1-ccde-492a-a73b-7fe177ca2e1e");
                    request_PutMaskingEmailAddress.Headers.Add("Postman-Token", "fb8ee0fd-64cc-4270-9e52-501fbd3aee6d,4cce079a-212b-4be6-870c-0ed639e372bc");
                    request_PutMaskingEmailAddress.Headers.Add("Cache-Control", "no-cache");
                    request_PutMaskingEmailAddress.Headers.Add("Authorization", "Zoho-oauthtoken " + AccessToken);
                    request_PutMaskingEmailAddress.Headers.Add("scope", "ZohoCRM.modules.custom.all,ZohoCRM.modules.contacts.all,ZohoCRM.modules.accounts.all,ZohoCRM.modules.deals.all,ZohoCRM.modules.events.all,ZohoCRM.modules.tasks.all,ZohoCRM.modules.calls.all,ZohoCRM.modules.invoices.all,ZohoCRM.modules.pricebooks.all,ZohoCRM.modules.salesorders.all,ZohoCRM.modules.purchaseorders.all,ZohoCRM.modules.products.all,ZohoCRM.modules.cases.all,ZohoCRM.modules.solutions.all,ZohoCRM.modules.vendors.all,ZohoCRM.modules.quotes.all,ZohoCRM.modules.ALL,ZohoCRM.settings.ALL,ZohoCRM.users.ALL,ZohoCRM.org.ALL,aaaserver.profile.ALL,ZohoCRM.settings.functions.all,ZohoCRM.functions.execute.read,ZohoCRM.functions.execute.create,ZohoCRM.settings.layout_rules.read,ZohoCRM.notifications.all,ZohoCRM.coql.READ");

                    using (var streamWriter = new StreamWriter(request_PutMaskingEmailAddress.GetRequestStream()))
                    {

                        string json = "";

                        // Sensible a mayucuslas y minusculas en los campos de Zoho
                        json = "{\r\n  \"data\": [\r\n    {\r\n      \"Secondary_Email_Address\": \" " + result_Email + " \" \r\n    }\r\n  ]\r\n}";


                        streamWriter.Write(json);

                    }

                    WebResponse webResponse_PutMaskingEmailAddress = request_PutMaskingEmailAddress.GetResponse();
                    Stream webStream_PutMaskingEmailAddress = webResponse_PutMaskingEmailAddress.GetResponseStream();
                    StreamReader responseReader_PutMaskingEmailAddress = new StreamReader(webStream_PutMaskingEmailAddress);
                    string response_PutMaskingEmailAddress = responseReader_PutMaskingEmailAddress.ReadToEnd();
                    dynamic data_PutMaskingEmailAddress = JObject.Parse(response_PutMaskingEmailAddress);


                    // Imprimimos el resultado del Update de Zoho
                    //Console.WriteLine("Mask - Update Zoho Result Secondary_Email_Address: ");
                    //Console.WriteLine(data_PutMaskingEmailAddress);


                    // Escribimos en el archivo el resultado del Update de Zoho
                    //File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Se ejecuto el Update de Zoho CRM.");
                    //File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " " + data_PutMaskingEmailAddress);


                }
                catch (Exception e)
                {
                    Console.Out.WriteLine(e.Message);
                    File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Error MaskingAPI: " + e);

                }



            }


        }

        // Imprimimos el resultado del Update de Zoho
        Console.WriteLine("");
        Console.WriteLine("######################################################");
        Console.WriteLine("Se termino de ejecutar el enmascaramiento de Secondary Emails.");
        Console.WriteLine("######################################################");

        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " ######################################################.");
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Se termino de ejecutar el enmascaramiento de Secondary Email.");
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " ######################################################.");

    }
    catch (Exception e)
    {

        Console.Out.WriteLine(e.Message);
        File.AppendAllText(path + "\\" + DateTime.Now.ToString("ddMMyyyy") + ".txt", Environment.NewLine + DateTime.Now.ToString("dd/MM/yyyy hh:mm:ss") + " Error en el SQL: " + e);

    }












} while (Unmasking_Avails == 1 && Resources.Mask_All == "1");



// Evitar que la consola se cierre inmediatamente
Console.WriteLine("");
Console.WriteLine("Presiona Enter para salir...");
Console.ReadLine();  // Espera que el usuario presione Enter