Imports OpenTK.Graphics.OpenGL
Imports System.IO

''' <summary>
''' ChipGL - A Chip-8 emulator/interpreter coded in VB.NET using OpenGL for video output
''' Copyright © Neethan Puvanendran 2013. All rights reserved.
''' </summary>
Public Class Form1
    Dim OpenGL_Loaded As Boolean = False        ' Has OpenGL initialized?
    Dim Emulator As New Chip8()                 ' The emulator

    Private Sub GlControl1_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles GlControl1.Load
        OpenGL_Loaded = True                    ' OpenGL has loaded
        GL.ClearColor(Color.Black)              ' Clear screen
        Setup()                                 ' Setup screen
    End Sub

    Sub Setup()
        Dim w = GlControl1.Width                ' Width and height of the screen
        Dim h = GlControl1.Height
        GL.MatrixMode(MatrixMode.Projection)    ' Set current matrix
        GL.LoadIdentity()                       ' Replace matrix
        GL.Ortho(0, w, h, 0, 0, 1)              ' Do some more matrix stuff (I don't really know what's going on here)
        'GL.Viewport(0, 0, w, h)                ' No need to set the viewport, works fine without it.
        GL.MatrixMode(MatrixMode.Modelview)     ' Set matrix mode
        GL.Disable(EnableCap.DepthTest)         ' 2D rendering
    End Sub

    Private Sub GlControl1_Paint(ByVal sender As Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles GlControl1.Paint
        If Not OpenGL_Loaded Then : Return : End If
        GL.Clear(ClearBufferMask.ColorBufferBit)    ' Clear buffer
        GL.MatrixMode(MatrixMode.Modelview)
        GL.LoadIdentity()
        If Emulator.Repaint Then                    ' If we need to repaint
            For ypos = 0 To 31
                For xpos = 0 To 63
                    If Emulator.ScreenData(ypos * 64 + xpos) = 1 Then   ' Pixel is set
                        GL.Color3(Color.White)
                        GL.Begin(BeginMode.Quads)
                        GL.Vertex2(xpos * 4 + 0.5, ypos * 4 - 0.5)
                        GL.Vertex2(xpos * 4 + 4 + 0.5, ypos * 4 - 0.5)
                        GL.Vertex2(xpos * 4 + 4 + 0.5, ypos * 4 + 4 - 0.5)
                        GL.Vertex2(xpos * 4 + 0.5, ypos * 4 + 4 - 0.5)
                        GL.End()
                    ElseIf Emulator.ScreenData(ypos * 64 + xpos) = 0 Then   ' Pixel isn't set
                        GL.Color3(Color.Black)
                        GL.Begin(BeginMode.Quads)
                        GL.Vertex2(xpos * 4 + 0.5, ypos * 4 - 0.5)          ' +0.5/-0.5 is for pixel precision.
                        GL.Vertex2(xpos * 4 + 4 + 0.5, ypos * 4 - 0.5)      ' I forget where I read it,
                        GL.Vertex2(xpos * 4 + 4 + 0.5, ypos * 4 + 4 - 0.5)  ' but I definitely remember reading it somewhere :P
                        GL.Vertex2(xpos * 4 + 0.5, ypos * 4 + 4 - 0.5)      ' Works fine on my nVidia GFX card.
                        GL.End()
                    End If
                Next
            Next
            Emulator.Repaint = False    ' Repaint is finished
        End If
        If Emulator.DoClearScreen Then  ' If we need to clear the screen
            GL.ClearColor(Color.Black)
            Emulator.DoClearScreen = False
        End If
        GlControl1.SwapBuffers()    ' Show the screen
    End Sub

    Private Sub BackgroundChecks_Tick(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BackgroundChecks.Tick
        If Emulator.DelayTimer > 0 Then
            Emulator.DelayTimer -= 1    ' Timer's tick is set to 17 ms, so decreases by 1 ~60 times a second.
        End If
        If Emulator.SoundTimer > 0 Then
            If Not SoundThread.IsBusy Then
                SoundThread.RunWorkerAsync(Emulator.SoundTimer * 16.667)    ' Beep!
            End If
        End If
        If Emulator.Repaint Then
            GlControl1.Invalidate()     ' Invalidating the control will fire the Paint() sub
        End If
        Emulator.KeyPressFired = False  ' No keys have been fired
        If Emulator.Emulating = False Then
            If SoundThread.IsBusy Then
                SoundThread.CancelAsync()
            End If
            BackgroundChecks.Stop()
        End If
    End Sub

    Private Sub Emulation_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles Emulation.DoWork
        While Emulator.Emulating = True
            Emulator.EmulateCycle()                         ' Emulate cycle
            Threading.Thread.Sleep(e.Argument(0))    ' Set to slow down/speed up the game. A setting of 1 or 2 works best for most games
        End While
    End Sub

    Private Sub Button1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button1.Click
        Dim o As New OpenFileDialog
        With o
            .Filter = "Chip-8 ROMS (*.ch8)|*.ch8|All files (*.*)|*.*"
            .Title = "Load ROM"
            .ValidateNames = True
            .RestoreDirectory = True
            .Multiselect = False
            .CheckFileExists = True
            .CheckPathExists = True
        End With
        If o.ShowDialog = Windows.Forms.DialogResult.OK AndAlso File.Exists(o.FileName) Then
            Emulator.Emulating = False  ' Stop emulation

            If SoundThread.IsBusy Then
                SoundThread.CancelAsync()   ' Stop sound thread
            End If
            BackgroundChecks.Stop()     ' Stop timer

            Emulator = New Chip8(o.FileName)    ' Set ROM
            Emulator.Initialize()       ' Initalize Chip-8

            Dim fs As New FileStream(Emulator.ROM, FileMode.Open, FileAccess.Read, FileShare.Read)
            fs.Seek(0, SeekOrigin.Begin)
            For a = 0 To fs.Length - 1              ' Load game into memory, TODO: check if the file size is too big for the memory
                Emulator.Memory(512 + a) = fs.ReadByte
            Next
            Do While Emulation.IsBusy           ' Wait for the Emulation thread to end.
                Application.DoEvents()
            Loop
            Emulator.Emulating = True           ' We are now emulating...
            Emulation.RunWorkerAsync(New Object() {NumericUpDown1.Value})          ' Start emulation thread
            BackgroundChecks.Start()            ' Start timer
        End If
    End Sub

    Private Sub Button2_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles Button2.Click
        Emulation.CancelAsync()                 ' Stop Emulator
        If SoundThread.IsBusy Then
            SoundThread.CancelAsync()
        End If
        BackgroundChecks.Stop()
        Emulator.Emulating = False
    End Sub

    Private Sub Form1_FormClosing(ByVal sender As Object, ByVal e As System.Windows.Forms.FormClosingEventArgs) Handles Me.FormClosing
        BackgroundChecks.Stop()
        Emulator.Emulating = False
        Emulation.CancelAsync()
        If SoundThread.IsBusy Then
            SoundThread.CancelAsync()
        End If
    End Sub

    Private Sub GlControl1_KeyUp(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles MyBase.KeyUp
        If e.KeyCode = Windows.Forms.Keys.D1 Then       ' Handle keypresses
            Emulator.Keys(1) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.D2 Then
            Emulator.Keys(2) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.D3 Then
            Emulator.Keys(3) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.D4 Then
            Emulator.Keys(12) = False


        ElseIf e.KeyCode = Windows.Forms.Keys.Q Then
            Emulator.Keys(4) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.W Then
            Emulator.Keys(5) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.E Then
            Emulator.Keys(6) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.R Then
            Emulator.Keys(13) = False


        ElseIf e.KeyCode = Windows.Forms.Keys.A Then
            Emulator.Keys(7) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.S Then
            Emulator.Keys(8) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.D Then
            Emulator.Keys(9) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.F Then
            Emulator.Keys(14) = False


        ElseIf e.KeyCode = Windows.Forms.Keys.Z Then
            Emulator.Keys(10) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.X Then
            Emulator.Keys(0) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.C Then
            Emulator.Keys(11) = False
        ElseIf e.KeyCode = Windows.Forms.Keys.V Then
            Emulator.Keys(15) = False
        End If
    End Sub

    Private Sub GlControl1_KeyDown(ByVal sender As Object, ByVal e As System.Windows.Forms.KeyEventArgs) Handles MyBase.KeyDown
        If e.KeyCode = Windows.Forms.Keys.D1 Then       ' Handle key presses
            Emulator.Keys(1) = True
            Emulator.LastKeyPress = 1                   ' Used for opcode Fx0A
            Emulator.KeyPressFired = True               ' Used for opcode Fx0A
        ElseIf e.KeyCode = Windows.Forms.Keys.D2 Then
            Emulator.Keys(2) = True
            Emulator.LastKeyPress = 2
            Emulator.KeyPressFired = True
        ElseIf e.KeyCode = Windows.Forms.Keys.D3 Then
            Emulator.Keys(3) = True
            Emulator.LastKeyPress = 3
            Emulator.KeyPressFired = True
        ElseIf e.KeyCode = Windows.Forms.Keys.D4 Then
            Emulator.Keys(12) = True
            Emulator.LastKeyPress = 12
            Emulator.KeyPressFired = True


        ElseIf e.KeyCode = Windows.Forms.Keys.Q Then
            Emulator.Keys(4) = True
            Emulator.LastKeyPress = 4
            Emulator.KeyPressFired = True
        ElseIf e.KeyCode = Windows.Forms.Keys.W Then
            Emulator.Keys(5) = True
            Emulator.LastKeyPress = 5
            Emulator.KeyPressFired = True
        ElseIf e.KeyCode = Windows.Forms.Keys.E Then
            Emulator.Keys(6) = True
            Emulator.LastKeyPress = 6
            Emulator.KeyPressFired = True
        ElseIf e.KeyCode = Windows.Forms.Keys.R Then
            Emulator.Keys(13) = True
            Emulator.LastKeyPress = 13
            Emulator.KeyPressFired = True


        ElseIf e.KeyCode = Windows.Forms.Keys.A Then
            Emulator.Keys(7) = True
            Emulator.LastKeyPress = 7
            Emulator.KeyPressFired = True
        ElseIf e.KeyCode = Windows.Forms.Keys.S Then
            Emulator.Keys(8) = True
            Emulator.LastKeyPress = 8
            Emulator.KeyPressFired = True
        ElseIf e.KeyCode = Windows.Forms.Keys.D Then
            Emulator.Keys(9) = True
            Emulator.LastKeyPress = 9
            Emulator.KeyPressFired = True
        ElseIf e.KeyCode = Windows.Forms.Keys.F Then
            Emulator.Keys(14) = True
            Emulator.LastKeyPress = 14
            Emulator.KeyPressFired = True


        ElseIf e.KeyCode = Windows.Forms.Keys.Z Then
            Emulator.Keys(10) = True
            Emulator.LastKeyPress = 10
            Emulator.KeyPressFired = True
        ElseIf e.KeyCode = Windows.Forms.Keys.X Then
            Emulator.Keys(0) = True
            Emulator.LastKeyPress = 0
            Emulator.KeyPressFired = True
        ElseIf e.KeyCode = Windows.Forms.Keys.C Then
            Emulator.Keys(11) = True
            Emulator.LastKeyPress = 11
            Emulator.KeyPressFired = True
        ElseIf e.KeyCode = Windows.Forms.Keys.V Then
            Emulator.Keys(15) = True
            Emulator.LastKeyPress = 15
            Emulator.KeyPressFired = True
        End If
    End Sub

    Private Sub SoundThread_DoWork(ByVal sender As System.Object, ByVal e As System.ComponentModel.DoWorkEventArgs) Handles SoundThread.DoWork
        System.Console.Beep(500, e.Argument)            ' Beep!
    End Sub

    Private Sub SoundThread_RunWorkerCompleted(ByVal sender As Object, ByVal e As System.ComponentModel.RunWorkerCompletedEventArgs) Handles SoundThread.RunWorkerCompleted
        Emulator.SoundTimer = 0                         ' Reset soundtimer
    End Sub
End Class
