<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<MySync.Server.Models.LoginModel>" %>

<asp:Content ID="loginTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <%: (string)base.RouteData.Values["title"] %>
</asp:Content>

<asp:Content ID="loginContent" ContentPlaceHolderID="MainContent" runat="server">
    <br />
    <hgroup class="title">
        <h1>Log in</h1>
    </hgroup>

    <section id="loginForm">
        <br />
        Please login to the server management to setup a configuration or to change existing settings.<br />
        The credentials should be <b>Username: "admin"</b> / <b>Password "admin"</b> if you have not changed them.
        <br /><br />
        <% using (Html.BeginForm(new { ReturnUrl = ViewBag.ReturnUrl })) { %>
            <%: Html.AntiForgeryToken() %>
            <%: Html.ValidationSummary(true) %>

            <table>
                <tr>
                    <td><%: Html.LabelFor(m => m.UserName, new { @class = "checkbox" })%><b>:</b></td>
                    <td><%: Html.TextBoxFor(m => m.UserName) %></td>
                    <td><%: Html.ValidationMessageFor(m => m.UserName) %></td>
                </tr>
                <tr>
                    <td><%: Html.LabelFor(m => m.Password, new { @class = "checkbox" })%><b>:</b></td>
                    <td><%: Html.PasswordFor(m => m.Password) %></td>
                    <td><%: Html.ValidationMessageFor(m => m.Password) %></td>
                </tr>
            </table>
            <br />
            <input type="submit" value="Submit" />
        <% } %>
    </section>
</asp:Content>

<asp:Content ID="scriptsContent" ContentPlaceHolderID="ScriptsSection" runat="server">
    <%: Scripts.Render("~/bundles/jqueryval") %>
</asp:Content>
