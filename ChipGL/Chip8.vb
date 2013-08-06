''' <summary>
''' ChipGL - A Chip-8 emulator/interpreter coded in VB.NET using OpenGL for video output
''' Copyright © Neethan Puvanendran 2013. All rights reserved.
''' </summary>
Public Class Chip8
    Public Memory(4095) As Byte     ' Chip-8 has 4K of memory
    Public ReadOnly ROM As String            ' The loaded ROM's full filename
    Private V(15) As Byte           ' Chip-8 has 16 8-bit registers
    Private I As UShort             ' 16-bit register to store memory addresses (so only the lower 12 bits are used)
    Private PC As UShort            ' 16-bit program counter
    Private Stack(15) As UShort     ' 16-bit stack stores addresses (for returning to after a subroutine)
    Private StackPointer As Byte    ' 8-bit stack pointer (should be 4-bit, but whatever)
    Public SoundTimer As Byte       ' Sound timer, max value of 255 (decreases at a rate of 60hz, so max sound length = 4.25 seconds)
    Public DelayTimer As Byte       ' Delay timer, decreases by one at a rate of 60hz (~17ms)
    Public Keys(15) As Boolean      ' Stores the key pressed values
    Public KeyPressFired As Boolean = False ' Set to true when a key is pressed, used for opcode Fx0A
    Public LastKeyPress As String   ' Stores the last pressed key, used for opcode Fx0A
    Public ReadOnly ScreenData(64 * 32) As Integer  ' Array to hold the Screen information
    Public Repaint As Boolean = False   ' Set during opcode Dxyn to repaint the GLControl
    Private Characters() As Byte        ' Array to store the characters (is initialized in Initialize())
    Public DoClearScreen As Boolean = False ' Set during ClearScreen() to clear the GLControl 
    Public Emulating As Boolean = False ' Set to true when emulator is running

    ''' <summary>
    ''' Creates a new instance of a Chip8 Emulator
    ''' </summary>
    ''' <param name="rom">The ROM file to load</param>
    ''' <remarks></remarks>
    Public Sub New(ByVal rom As String)
        Me.ROM = rom
    End Sub

    ''' <summary>
    ''' Creates a new instance of a Chip8 Emulator
    ''' </summary>
    ''' <remarks>A ROM must be set by initializing the class again.</remarks>
    Public Sub New()

    End Sub

    ''' <summary>
    ''' Emulates a cycle
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub EmulateCycle()
        Dim opcode As String = Memory(PC).ToString("X").PadLeft(2, "0"c) & Memory(PC + 1).ToString("X").PadLeft(2, "0"c)    ' Get opcode
        ' The Chip-8 has 35 different opcodes. (36 if you include 0nnn - SYS addr, but it is ignored by modern interpreters)
        ' Each opcode is 2 bytes long
        ' Simple, right?
        If opcode = "00E0" Then ' 00E0, CLS
            ClearScreen()       ' Clears the screen
            PC += 2
        ElseIf opcode = "00EE" Then     ' 00EE, RET
            PC = Stack(StackPointer)    ' Returns from a subroutine
            StackPointer -= 1
            PC += 2
        ElseIf opcode.StartsWith("1") And opcode.Length = 4 Then        ' 1nnn, JP addr
            Dim addr As Integer = Val("&H" & opcode.Substring(1, 3))    ' Jumps to an address at nnn
            PC = addr
        ElseIf opcode.StartsWith("2") And opcode.Length = 4 Then     ' 2nnn, CALL addr
            Dim addr As Integer = Val("&H" & opcode.Substring(1, 3)) ' Calls a subroutine at nnn
            StackPointer += 1
            Stack(StackPointer) = PC
            PC = addr
        ElseIf opcode.StartsWith("3") And opcode.Length = 4 Then    ' 3xkk - SE Vx, byte
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))   ' Skip next opcode if the value in register Vx = kk
            Dim bite As Integer = Val("&H" & opcode.Substring(2, 2))
            If V(x) = bite Then
                PC += 4
            Else
                PC += 2
            End If
        ElseIf opcode.StartsWith("4") And opcode.Length = 4 Then    ' 4xkk - SNE Vx, byte
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))   ' Skip next opcode if the value in register Vx != kk
            Dim bite As Integer = Val("&H" & opcode.Substring(2, 2))
            If Not V(x) = bite Then
                PC += 4
            Else
                PC += 2
            End If
        ElseIf opcode.StartsWith("5") And opcode.Length = 4 Then    ' 5xy0 - SE Vx, Vy
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))   ' Compare the values in the registers Vx and Vy, skip opcode if they are equal
            Dim y As Integer = Val("&H" & opcode.Substring(2, 1))
            If V(x) = V(y) Then
                PC += 4
            Else
                PC += 2
            End If
        ElseIf opcode.StartsWith("6") And opcode.Length = 4 Then    ' 6xkk - LD Vx, byte
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))   ' Load kk into register Vx
            Dim bite As Byte = Val("&H" & opcode.Substring(2, 2))
            V(x) = bite
            PC += 2
        ElseIf opcode.StartsWith("7") And opcode.Length = 4 Then    ' 7xkk - ADD Vx, byte
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))   ' Add kk to Vx
            Dim bite As Byte = Val("&H" & opcode.Substring(2, 2))
            Dim compare As Integer = CInt(V(x)) + CInt(bite)        ' VB.NET doesn't wrap bytes like C#.
            If Not compare < 256 Then : compare -= 256 : End If     ' You can do 255 + 1 = 0 in C#, but VB.NET throws an exception
            V(x) = compare
            PC += 2
        ElseIf opcode.StartsWith("8") And opcode.Length = 4 And opcode.EndsWith("0") Then   ' 8xy0 - LD Vx, Vy
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Copy the value of register Vy and put it in Vx
            Dim y As Integer = Val("&H" & opcode.Substring(2, 1))
            V(x) = V(y)
            PC += 2
        ElseIf opcode.StartsWith("8") And opcode.Length = 4 And opcode.EndsWith("1") Then   ' 8xy1 - OR Vx, Vy
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Do a bitwise OR on Vx and Vy.
            Dim y As Integer = Val("&H" & opcode.Substring(2, 1))
            Dim value As Byte = V(x) Or V(y)    ' Thank goodness VB.NET has bitwise operators
            V(x) = value
            PC += 2
        ElseIf opcode.StartsWith("8") And opcode.Length = 4 And opcode.EndsWith("2") Then   ' 8xy2 - AND Vx, Vy
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Do a bitwise AND on Vx and Vy
            Dim y As Integer = Val("&H" & opcode.Substring(2, 1))
            Dim value As Byte = V(x) And V(y)
            V(x) = value
            PC += 2
        ElseIf opcode.StartsWith("8") And opcode.Length = 4 And opcode.EndsWith("3") Then   ' 8xy3 - XOR Vx, Vy
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Do a bitwise XOR on Vx and Vy
            Dim y As Integer = Val("&H" & opcode.Substring(2, 1))
            Dim value As Byte = V(x) Xor V(y)
            V(x) = value
            PC += 2
        ElseIf opcode.StartsWith("8") And opcode.Length = 4 And opcode.EndsWith("4") Then   ' 8xy4 - ADD Vx, Vy
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Add Vy to Vx, set VF is there is a carry
            Dim y As Integer = Val("&H" & opcode.Substring(2, 1))
            Dim value As Integer = CInt(V(x)) + CInt(V(y))      ' VB.NET stupidness again
            If value > 255 Then
                V(15) = 1
                Dim bite As Byte = Val("&H" & value.ToString("X").Substring(1, 2))
                V(x) = bite
            Else
                V(15) = 0
                V(x) = value
            End If
            PC += 2
        ElseIf opcode.StartsWith("8") And opcode.Length = 4 And opcode.EndsWith("5") Then   ' 8xy5 - SUB Vx, Vy
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Subtract Vy from Vx, store in Vx, set VF if there isn't a borrow
            Dim y As Integer = Val("&H" & opcode.Substring(2, 1))
            If V(x) >= V(y) Then
                V(15) = 1
                V(x) = V(x) - V(y)
            ElseIf Not V(x) >= V(y) Then
                V(15) = 0
                Dim value As Integer = (CInt(V(x)) + 256) - CInt(V(y))          ' Sigh...
                V(x) = value
            End If
            PC += 2
        ElseIf opcode.StartsWith("8") And opcode.Length = 4 And opcode.EndsWith("6") Then   ' 8xy6 - SHR Vx
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Bitshift Vx to the right by 1
            Dim binary As String = Convert.ToString(V(x), 2).PadLeft(8, "0"c)   ' Convert to binary
            If binary.EndsWith("1") Then
                V(15) = 1
            Else
                V(15) = 0
            End If

            V(x) >>= 1      ' Bitshift by 1 to the right
            PC += 2
        ElseIf opcode.StartsWith("8") And opcode.Length = 4 And opcode.EndsWith("7") Then   ' 8xy7 - SUBN Vx, Vy
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Subtract Vx from Vy, store in Vx, set VF is there isn't a borrow
            Dim y As Integer = Val("&H" & opcode.Substring(2, 1))
            If V(y) >= V(x) Then
                V(15) = 1
                V(x) = V(y) - V(x)
            ElseIf Not V(y) >= V(x) Then
                V(15) = 0
                Dim value As Integer = (CInt(V(y)) + 256) - CInt(V(x))          ' Sigh...
                V(x) = value
            End If
            PC += 2
        ElseIf opcode.StartsWith("8") And opcode.Length = 4 And opcode.EndsWith("E") Then   ' 8xyE - SHL Vx {, Vy}
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Bitshift Vx to the left by 1
            Dim binary As String = Convert.ToString(V(x), 2).PadLeft(8, "0"c)   ' Convert to binary
            If binary.StartsWith("1") Then
                V(15) = 1
            Else
                V(15) = 0
            End If
            V(x) <<= 1      ' Bitshift by 1 to the left
            PC += 2
        ElseIf opcode.StartsWith("9") And opcode.Length = 4 Then    ' 9xy0 - SNE Vx, Vy
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))   ' Skip next opcode if Vx != Vy
            Dim y As Integer = Val("&H" & opcode.Substring(2, 1))
            If Not V(x) = V(y) Then
                PC += 4
            Else
                PC += 2
            End If
        ElseIf opcode.StartsWith("A") And opcode.Length = 4 Then        ' Annn - LD I, addr
            Dim addr As Integer = Val("&H" & opcode.Substring(1, 3))    ' Set I register to nnn
            I = addr
            PC += 2
        ElseIf opcode.StartsWith("B") And opcode.Length = 4 Then        ' Bnnn - JP V0, addr
            Dim addr As Integer = Val("&H" & opcode.Substring(1, 3))    ' Set PC to nnn + the value of V0
            PC = addr + V(0)
        ElseIf opcode.StartsWith("C") And opcode.Length = 4 Then        ' Cxkk - RND Vx, byte
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))       ' Set Vx = random byte (0-255) AND kk
            Dim bite As Byte = Val("&H" & opcode.Substring(2, 2))
            Dim r As New Random
            Dim value = r.Next(0, 255) And bite
            V(x) = value
            PC += 2
        ElseIf opcode.StartsWith("D") And opcode.Length = 4 Then        ' Dxyn - DRW Vx, Vy, nibble
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))       ' Draw a n-byte sprite (which is n pixels tall) starting at the memory address in I, at position Vx, Vy
            Dim y As Integer = Val("&H" & opcode.Substring(2, 1))       ' Set VF is there is sprite collision
            Dim height As Integer = Val("&H" & opcode.Substring(3, 1))
            Dim sprite_x As Byte = V(x)
            Dim sprite_y As Byte = V(y)
            V(15) = 0
            Dim pixel As Integer
            For yline = 0 To height - 1
                Dim pixel_Y = (sprite_y + yline)
                pixel = Memory(I + yline)   ' Get byte from memory
                For xline = 0 To 7
                    Dim pixel_X = (sprite_x + xline)
                    Dim b As String = Convert.ToString(pixel, 2).PadLeft(8, "0"c).Substring(xline, 1) ' Get binary byte
                    If b = "1" Then ' If we should draw a pixel...
                        If DrawPixel(pixel_X, pixel_Y) = 0 Then ' Draw a pixel, if a pixel was removed...
                            V(15) = 1   ' Set collision
                        End If
                    End If
                Next
            Next
            Repaint = True  ' Tell OpenGL to redraw.
            PC += 2
        ElseIf opcode.StartsWith("E") And opcode.Length = 4 And opcode.EndsWith("9E") Then  ' Ex9E - SKP Vx
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Skip opcode if key (Vx) is pressed
            If Keys(V(x)) = True Then
                PC += 4
            Else
                PC += 2
            End If
        ElseIf opcode.StartsWith("E") And opcode.Length = 4 And opcode.EndsWith("A1") Then  ' ExA1 - SKNP Vx
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Skip opcode if key (Vx) is not pressed
            If Keys(V(x)) = False Then
                PC += 4
            Else
                PC += 2
            End If
        ElseIf opcode.StartsWith("F") And opcode.Length = 4 And opcode.EndsWith("07") Then  ' Fx07 - LD Vx, DT
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Set Vx = delay timer value
            V(x) = DelayTimer
            PC += 2
        ElseIf opcode.StartsWith("F") And opcode.Length = 4 And opcode.EndsWith("0A") Then  ' Fx0A - LD Vx, K
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Wait for key press, store into Vx
            While Emulating = True
                If KeyPressFired = True Then
                    V(x) = LastKeyPress         ' Kinda hacky, but I can't think of a better way
                    Exit While
                End If
            End While
            PC += 2
        ElseIf opcode.StartsWith("F") And opcode.Length = 4 And opcode.EndsWith("15") Then  ' Fx15 - LD DT, Vx
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Set delay timer to value of Vx
            DelayTimer = V(x)
            PC += 2
        ElseIf opcode.StartsWith("F") And opcode.Length = 4 And opcode.EndsWith("18") Then  ' Fx18 - LD ST, Vx
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Set sound timer to value of Vx
            SoundTimer = V(x)
            PC += 2
        ElseIf opcode.StartsWith("F") And opcode.Length = 4 And opcode.EndsWith("1E") Then  ' Fx1E - ADD I, Vx
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Add Vx to register I
            If I + V(x) > 4095 Then     ' Stupid VB.NET :(
                I = I + V(x) - 4096
            Else
                I += V(x)
            End If
            PC += 2
        ElseIf opcode.StartsWith("F") And opcode.Length = 4 And opcode.EndsWith("29") Then  ' Fx29 - LD F, Vx
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Set I to the location of the sprite for digit Vx
            Select Case V(x)
                Case 0
                    I = 0
                Case 1
                    I = 5
                Case 2
                    I = 10
                Case 3
                    I = 15
                Case 4
                    I = 20
                Case 5
                    I = 25
                Case 6
                    I = 30
                Case 7
                    I = 35
                Case 8
                    I = 40
                Case 9
                    I = 45
                Case 10
                    I = 50
                Case 11
                    I = 55
                Case 12
                    I = 60
                Case 13
                    I = 65
                Case 14
                    I = 70
                Case 15
                    I = 75
            End Select
            PC += 2
        ElseIf opcode.StartsWith("F") And opcode.Length = 4 And opcode.EndsWith("33") Then  ' Fx33 - LD B, Vx
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Convert a hex number to decimal
            Memory(I) = V(x) / 100                  ' Store hundreds in I
            Memory(I + 1) = (V(x) / 10) Mod 10      ' Tens in I + 1
            Memory(I + 2) = (V(x) Mod 100) Mod 10   ' And ones in I + 2
            PC += 2
        ElseIf opcode.StartsWith("F") And opcode.Length = 4 And opcode.EndsWith("55") Then  ' Fx55 - LD [I], Vx
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Store registers V0 to Vx in memory starting at I
            For a = 0 To x
                Memory(I + a) = V(a)
            Next
            PC += 2
        ElseIf opcode.StartsWith("F") And opcode.Length = 4 And opcode.EndsWith("65") Then  ' Fx65 - LD Vx, [I]
            Dim x As Integer = Val("&H" & opcode.Substring(1, 1))                           ' Read registers V0 to Vx from memory starting at I
            For a = 0 To x
                V(a) = Memory(I + a)
            Next
            PC += 2
        Else
            Emulating = False
            MsgBox("Invalid opcode: " & opcode & vbNewLine & "PC: " & PC & vbNewLine & "I: " & I & vbNewLine, MsgBoxStyle.OkOnly, "ChipGL Error")
        End If
    End Sub

    ''' <summary>
    ''' Clears the screen
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub ClearScreen()
        For d = 0 To 2048       ' Clear screen
            ScreenData(d) = 0
        Next
        DoClearScreen = True    ' Tell graphics renderer to clear the screen
    End Sub

    ''' <summary>
    ''' Initializes the Chip-8
    ''' </summary>
    ''' <remarks></remarks>
    Public Sub Initialize()
        If ROM = Nothing Or ROM = String.Empty Then
            Throw New Exception("No ROM has been loaded!")
        End If
        PC = 512                ' Reset program counter
        I = 0                   ' Reset address loader
        StackPointer = 0        ' Reset stack pointer
        ClearScreen()           ' Clear screen
        For a = 0 To 15         ' Clear stack, registers and key values
            Stack(a) = 0
            V(a) = 0
            Keys(a) = False
        Next
        For a = 0 To 4095       ' Clear memory
            Memory(a) = 0
        Next
        Characters = {
                        &HF0, &H90, &H90, &H90, &HF0,
                        &H20, &H60, &H20, &H20, &H70,
                        &HF0, &H10, &HF0, &H80, &HF0,
                        &HF0, &H10, &HF0, &H10, &HF0,
                        &H90, &H90, &HF0, &H10, &H10,
                        &HF0, &H80, &HF0, &H10, &HF0,
                        &HF0, &H80, &HF0, &H90, &HF0,
                        &HF0, &H10, &H20, &H40, &H40,
                        &HF0, &H90, &HF0, &H90, &HF0,
                        &HF0, &H90, &HF0, &H10, &HF0,
                        &HF0, &H90, &HF0, &H90, &H90,
                        &HE0, &H90, &HE0, &H90, &HE0,
                        &HF0, &H80, &H80, &H80, &HF0,
                        &HE0, &H90, &H90, &H90, &HE0,
                        &HF0, &H80, &HF0, &H80, &HF0,
                        &HF0, &H80, &HF0, &H80, &H80
                    }           ' Set font
        For a = 0 To 79         ' Load font into memory
            Memory(a) = Characters(a)
        Next
        SoundTimer = 0          ' Reset timers
        DelayTimer = 0
    End Sub

    ''' <summary>
    ''' XORs a pixel to the screen
    ''' </summary>
    ''' <param name="x">X position of pixel</param>
    ''' <param name="y">Y position of pixel</param>
    ''' <returns>Value of the set pixel</returns>
    ''' <remarks></remarks>
    Private Function DrawPixel(ByVal x As Integer, ByVal y As Integer)
        x = x Mod 64    ' If X or Y is greater than 64 or 32 respectively, then the pixels must wrap around the screen
        y = y Mod 32
        ScreenData(x + (y * 64)) = ScreenData(x + (y * 64)) Xor 1   ' Pixel is XORed to the screen
        Return ScreenData(x + (y * 64))
    End Function
End Class
