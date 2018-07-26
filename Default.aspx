<%@ Page Language="C#" AutoEventWireup="True" CodeBehind="Default.aspx.cs" Inherits="Color2Gray.Default" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
        <div>
            <p><b>Source: </b><asp:FileUpload ID="sourceFU" runat="server" /></p>
            <asp:ScriptManager ID="ScriptManager1" runat="server" />
            <asp:UpdatePanel runat="server" ID="imageUP">
                <Triggers>
                    <asp:PostBackTrigger ControlID="runBtn" />
                    <asp:PostBackTrigger ControlID="clearBtn" />
                </Triggers>
                <ContentTemplate>
                    <p>
                        <b>Task: </b>
                        <asp:DropDownList ID="taskDDL" runat="server" OnSelectedIndexChanged="showOptions" AutoPostBack="true">
                            <asp:ListItem Text="Grayscale (Averaging)" Value="grayscaleAverage"></asp:ListItem>
                            <asp:ListItem Text="Grayscale (BT.601)" Value="grayscaleBT601"></asp:ListItem>
                            <asp:ListItem Text="Grayscale (Desaturation)" Value="grayscaleDesaturation"></asp:ListItem>
                            <asp:ListItem Text="Grayscale via LAB" Value="grayscaleLab"></asp:ListItem>
                            <asp:ListItem Text="Color2Gray" Value="color2Gray"></asp:ListItem>
                            <asp:ListItem Text="Color2Gray+Color" Value="color2Color"></asp:ListItem>
                            <asp:ListItem Text="Color2Gray+Color+Correction" Value="color2CC"></asp:ListItem>
                        </asp:DropDownList>
                    </p>
                    <asp:Panel ID="C2GPanel" runat="server" Visible="false">
                        <p><b>Radius: </b><asp:TextBox ID="RadiusTB" TextMode="Number" runat="server" min="0" max="500" step="1" Text="10" /></p>
                        <p><b>Alpha: </b><asp:TextBox ID="AlphaTB" TextMode="Number" runat="server" min="0" max="50" step="1" Text="10" /></p>
                        <p><b>Theta: </b><asp:TextBox ID="ThetaTB" TextMode="Number" runat="server" min="0" max="359" step="1" Text="45" /></p>
                        <p><b>Iterations: </b><asp:TextBox ID="ItersTB" TextMode="Number" runat="server" min="0" max="50" step="1" Text="10" /></p>
                        <asp:Panel ID="CBPanel" runat="server" Visible="false">
                            <p>
                                <b>Blindness Correction: </b>
                                <asp:DropDownList ID="CBDDL" runat="server">
                                    <asp:ListItem Text="Deuteranopia: green weakness" Value="1"></asp:ListItem>
                                    <asp:ListItem Text="Protanopia: red weakness" Value="2"></asp:ListItem>
                                    <asp:ListItem Text="Tritanopia: blue weakness" Value="3"></asp:ListItem>
                                </asp:DropDownList>
                            </p>
                        </asp:Panel>
                    </asp:Panel>
                    <asp:Button ID="runBtn" Text="Run!" OnClick="run" runat="server" />
                    <asp:Button ID="clearBtn" Text="Clear!" OnClick="clear" runat="server" />
                    <hr />
                    <asp:Label ID="debug" runat="server" />
                </ContentTemplate>
            </asp:UpdatePanel>
        </div>
    </form>
</body>
</html>
