﻿<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage<MySync.Server.Models.SetupModel>" %>

<asp:Content ID="setupTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <%: (string)base.RouteData.Values["title"] %>
</asp:Content>

<asp:Content ID="setupContent" ContentPlaceHolderID="MainContent" runat="server">
    <br />
    <hgroup class="title">
        <h1>Setup server entry point</h1>
    </hgroup>

    <section id="setupForm">
        <br />
        <% using (Html.BeginForm(new { ReturnUrl = ViewBag.ReturnUrl })) { %>   
            <table>
                <tr>
                    <td><%: Html.LabelFor(m => m.DataProfile, new { @class = "checkbox" }) %><b>:</b></td>
                    <td style="height:50px"><%: Html.DropDownListFor(m => m.DataProfile, new SelectList(ViewBag.DPClasses, "Key", "Value"), new { style = "width:312px;height:35px" })%></td>
                    <td><%: Html.ValidationMessageFor(m => m.DataProfile) %></td>
                </tr>
                <!--<tr>
                    <td><%: Html.LabelFor(m => m.Path, new { @class = "checkbox" }) %><b>:</b></td>
                    <td><%: Html.TextBoxFor(m => m.Path) %></td>
                    <td><%: Html.ValidationMessageFor(m => m.Path) %></td>
                </tr>-->
                <tr>
                    <td><%: Html.LabelFor(m => m.DiskSpace, new { @class = "checkbox" }) %><b>:</b></td>
                    <td><%: Html.TextBoxFor(m => m.DiskSpace)%>&nbsp;&nbsp;Mb</td>
                    <td><%: Html.ValidationMessageFor(m => m.DiskSpace) %></td>
                </tr>
                <tr>
                    <td style="width:250px"><%: Html.LabelFor(m => m.FileSize, new { @class = "checkbox" }) %><b>: *</b></td>
                    <td style="width:400px"><%: Html.TextBoxFor(m => m.FileSize) %>&nbsp;&nbsp;&lt;=&nbsp;<%: ViewBag.MaxFileSize %>&nbsp;Mb</td>
                    <td style="width:300px"><%: Html.ValidationMessageFor(m => m.FileSize) %></td>
                </tr>
                <tr>
                    <td></td>
                    <td style="font-size:10pt;">* max value defined in Web.config</td>
                </tr>
                <tr><td></td></tr>
                <tr><td></td></tr>
                <tr><td></td></tr>
                <tr>
                    <td><b>Entry Point URL for client:</b></td>
                    <td><%: ViewBag.SyncURL %></td>
                </tr>
            </table>
        <br />
            <input type="submit" value="Save" />
        <% } %>
    </section>
</asp:Content>

<asp:Content ID="scriptsContent" ContentPlaceHolderID="ScriptsSection" runat="server">
    <%: Scripts.Render("~/bundles/jqueryval") %>
</asp:Content>