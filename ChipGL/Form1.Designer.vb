<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form1
    Inherits System.Windows.Forms.Form

    'Form overrides dispose to clean up the component list.
    <System.Diagnostics.DebuggerNonUserCode()> _
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    'Required by the Windows Form Designer
    Private components As System.ComponentModel.IContainer

    'NOTE: The following procedure is required by the Windows Form Designer
    'It can be modified using the Windows Form Designer.  
    'Do not modify it using the code editor.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.Panel1 = New System.Windows.Forms.Panel()
        Me.GlControl1 = New OpenTK.GLControl()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Emulation = New System.ComponentModel.BackgroundWorker()
        Me.BackgroundChecks = New System.Windows.Forms.Timer(Me.components)
        Me.SoundThread = New System.ComponentModel.BackgroundWorker()
        Me.NumericUpDown1 = New System.Windows.Forms.NumericUpDown()
        Me.Panel1.SuspendLayout()
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'Panel1
        '
        Me.Panel1.Controls.Add(Me.GlControl1)
        Me.Panel1.Location = New System.Drawing.Point(12, 12)
        Me.Panel1.Name = "Panel1"
        Me.Panel1.Size = New System.Drawing.Size(256, 128)
        Me.Panel1.TabIndex = 0
        '
        'GlControl1
        '
        Me.GlControl1.BackColor = System.Drawing.Color.Black
        Me.GlControl1.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GlControl1.Location = New System.Drawing.Point(0, 0)
        Me.GlControl1.Name = "GlControl1"
        Me.GlControl1.Size = New System.Drawing.Size(256, 128)
        Me.GlControl1.TabIndex = 0
        Me.GlControl1.VSync = False
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(12, 146)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(256, 23)
        Me.Button1.TabIndex = 1
        Me.Button1.Text = "Load Game"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(12, 175)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(256, 23)
        Me.Button2.TabIndex = 2
        Me.Button2.Text = "Halt Emulator"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Emulation
        '
        Me.Emulation.WorkerSupportsCancellation = True
        '
        'BackgroundChecks
        '
        Me.BackgroundChecks.Interval = 17
        '
        'SoundThread
        '
        Me.SoundThread.WorkerSupportsCancellation = True
        '
        'NumericUpDown1
        '
        Me.NumericUpDown1.Location = New System.Drawing.Point(232, 204)
        Me.NumericUpDown1.Maximum = New Decimal(New Integer() {10, 0, 0, 0})
        Me.NumericUpDown1.Name = "NumericUpDown1"
        Me.NumericUpDown1.Size = New System.Drawing.Size(36, 20)
        Me.NumericUpDown1.TabIndex = 6
        Me.NumericUpDown1.Value = New Decimal(New Integer() {1, 0, 0, 0})
        '
        'Form1
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(280, 230)
        Me.Controls.Add(Me.NumericUpDown1)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.Panel1)
        Me.KeyPreview = True
        Me.Name = "Form1"
        Me.Text = "ChipGL"
        Me.Panel1.ResumeLayout(False)
        CType(Me.NumericUpDown1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents Panel1 As System.Windows.Forms.Panel
    Friend WithEvents Button1 As System.Windows.Forms.Button
    Friend WithEvents Button2 As System.Windows.Forms.Button
    Friend WithEvents GlControl1 As OpenTK.GLControl
    Friend WithEvents Emulation As System.ComponentModel.BackgroundWorker
    Friend WithEvents BackgroundChecks As System.Windows.Forms.Timer
    Friend WithEvents SoundThread As System.ComponentModel.BackgroundWorker
    Friend WithEvents NumericUpDown1 As System.Windows.Forms.NumericUpDown

End Class
