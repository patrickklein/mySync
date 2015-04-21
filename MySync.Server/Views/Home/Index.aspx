<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="indexTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <%: (string)base.RouteData.Values["title"] %>
</asp:Content>

<asp:Content ID="indexContent" ContentPlaceHolderID="MainContent" runat="server">
    <br />
    <hgroup class="title">
        <h1>How does it work:</h1>
    </hgroup>
    <ol class="round">
        <li class="one">
            <h5>Getting Started</h5>
            Congratulations, you have successfully installed and set up <b>mySync Server Management</b>. 
            Follow the next steps to setup your synchronization environment correctly. No warranties are given for anything.
            For a correct usage of mySync, please ensure you have installed a PostgreSQL database instance on your server, reachable with standard login credentials (xxx:xxx).
        </li>

        <!--<li class="two">
            <h5>Login at mySync server management and setup an entry point</h5>
            Go to the <%: Html.ActionLink("login section", "Login", "Account") %> and sign in on the server management with the provided credentials on the login page. (After signing in, you can change the password if you want to) Set up a new server entry point for the client application, by providing a valid path as data storage. The created server entry point is now available with following URI: <b>http://xxx.xxx.xxx.xxx/synchronization/entryPoint</b>
        </li>-->

        <li class="two">
            <h5>setup an entry point</h5>
            Go to the <%: Html.ActionLink("setup section", "Setup", "Account") %> and set up a new server entry point for the client application, by providing a valid path as data storage. The created server entry point is now available with the following URI: <b>http://xxx.xxx.xxx.xxx/Account/Upload</b>
        </li>

        <li class="three">
            <h5>Download mySync client application</h5>
            You have to download the <b>mySync client application</b> and install it on an appropriate machine. This application was developed and tested for usage on Microsoft Windows 7 (64-bit) systems, but should work with other Microsoft Windows Versions too.
            You also need to install <b>Microsofts .NET 4.5 Framework</b>, to get the client application run.<br /><br />
            <a href="http://github.com/mySync/mySync.Client" target="_blank">download client application…</a>
        </li>

        <li class="four">
            <h5>Configure mySync client application</h5>
            Start the mySync client application on the client/user machine and navigate to the <b>"Server" tab</b>. Add a new "Server Entry Point" by clicking the + sign in the bottom right and <b>paste the server entry URI from step two</b> into the <b>"Server Address" textbox</b>. Define which local folder should get sychronized with this server and save the changes. The client should start checking the files and folders and synchronizing the data to the server.
        </li>

        <li class="five">
            <h5>Enjoy...</h5>
            have fun and happy sharing your data with your own server
        </li>
    </ol>
</asp:Content>
