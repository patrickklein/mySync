<%@ Page Language="C#" MasterPageFile="~/Views/Shared/Site.Master" Inherits="System.Web.Mvc.ViewPage" %>

<asp:Content ID="aboutTitle" ContentPlaceHolderID="TitleContent" runat="server">
    <%: (string)base.RouteData.Values["title"] %>
</asp:Content>

<asp:Content ID="aboutContent" ContentPlaceHolderID="MainContent" runat="server">
    <br />
    <hgroup class="title">
        <h1>About mySync</h1>
    </hgroup>
    <br />
    <section class="contact">
        <span>This application was written for my master thesis project with the topic "synchronization algorithms" on the University of Applied Sciences Technikum Wien in Vienna, Austria. ...</span>
    </section>
    <br />
    <section class="contact">
        <header>
            <h3>Disclaimer:</h3>
        </header>
        <p>
            <span>&copy; Copyright  <%: DateTime.Now.Year %> - Patrick Klein - All Rights Reserved - No warranties are given.</span><br />
            This work is licensed under a <a rel="license" href="http://creativecommons.org/licenses/by-nc-sa/4.0/" target="_blank">Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License</a>.
            <br /><br />

            <span class="label">You are free to:</span><br />
            <span>— Share - copy and redistribute the material in any medium or format</span><br />
            <span>— Adapt - remix, transform, and build upon the material</span>
            <br />
            <br />
            <span class="label">Under the following terms:</span><br />
            <table style="margin-left:10px;">
                <tr>
                    <td><img src="/Images/license/Attribution.png" /></td>
                    <td><b>Attribution</b> - You must give <b><u>appropriate credit</u></b>, provide a link to the license, and <b><u>indicate if changes were made</u></b>. You may do so in any reasonable manner, but not in any way that suggests the licensor endorses you or your use.</td>
                </tr>
                <tr>
                    <td><img src="/Images/license/NonCommercial.png" /></td>
                    <td><b>NonCommercial</b> - You may <b><u>not</u></b> use the material for <b><u>commercial purposes</u></b>.</td>
                </tr>
                <tr>
                    <td><img src="/Images/license/ShareAlike.png" /></td>
                    <td><b>ShareAlike</b> - If you remix, transform, or build upon the material, you must distribute your contributions under the <b><u>same license</u></b> as the original.</td>
                </tr>
            </table>
            <br />
            <span><b>No additional restrictions</b> - You may not apply legal terms or <b><u>technological measures</u></b> that legally restrict others from doing anything the license permits.</span>
            
            <br /><br />
            <img alt="Creative Commons License" style="border-width:0" src="/Images/license/license.png" /> 
        </p>
    </section>
    <br /><br />
    <section class="contact">
        <header>
            <h3>Source Code:</h3>
        </header>
        <p>The complete source code of the client and server application are available under Creative Commons Attribution-NonCommercial-ShareAlike 4.0 International License on the following github reference:</p>
        <br />
        <table style="margin-left:10px;">
            <tr>
                <td style="width:60px" class="label">Client:</td>
                <td><a href="http://github.com/mySync" target="_blank">http://github.com/mySync/mySync.Client</a></td>
            </tr>
            <tr>
                <td class="label">Server:</td>
                <td><a href="http://github.com/mySync" target="_blank">http://github.com/mySync/mySync.Server</a></td>
            </tr>
        </table>
    </section>
    <br /><br />
    <section class="contact">
        <header>
            <h3>Contact:</h3>
        </header>
        <table style="margin-left:10px;">
            <tr>
                <td style="width:60px" class="label">Email:</td>
                <td>patrick.klein@technikum-wien.at</td>
            </tr>
            <tr>
                <td class="label">Message:</td>
                <td>leave me a message on github (mySync)</td>
            </tr>
        </table>
    </section>
</asp:Content>