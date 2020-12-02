Public Class VBShit
    Public Declare Function GetAsyncKeyState Lib "user32" _
                                    (ByVal vKey As Long) As Integer

    Public Declare Function ReadProcessMemory Lib "kernel32" _
                                    (ByVal hProcess As Integer,
                                     ByVal lpBaseAddress As Integer,
                                     ByRef lpBuffer As Long,
                                     ByVal nSize As Long,
                                     ByRef lpNumberOfBytesRead As Long) As Long

    Public Declare Sub mouse_event Lib "user32" Alias "mouse_event" _
                                    (ByVal dwFlags As Long,
                                     ByVal dx As Long,
                                     ByVal dy As Long,
                                     ByVal dwData As Long,
                                     ByVal dwExtraInfo As Long)

    'Constant definitions
    Public Const MOUSEEVENTF_LEFTDOWN = &H2
    Public Const MOUSEEVENTF_LEFTUP = &H4
    Public Const MOUSEEVENTF_MIDDLEDOWN = &H20
    Public Const MOUSEEVENTF_MIDDLEUP = &H40
    Public Const FL_ONGROUND = (1 << 0)

    'Threads
    ReadOnly TriggerThread As System.Threading.Thread = New System.Threading.Thread(AddressOf Triggerbot)
    ReadOnly BhopThread As System.Threading.Thread = New System.Threading.Thread(AddressOf BunnyHop)

    'Core cheat variables
    Public CSGOProc As Process()
    Public bShouldRun As Boolean = True 'Run loops as long as this is true
    ReadOnly szProcess As String = "csgo" 'The exename of our wanted process
    Public ClientDLL As Integer = 0 'Holds client modules baseaddr

    'Offsets
    Public Const m_dwLocalPlayer As Integer = &HD3ED14
    Public Const m_dwEntityList As Integer = &H4D533AC
    Public Const m_dwGlowObject As Integer = &H529B210

    'Netvars
    Public Const m_nCrosshairID As Integer = &HB3E4
    Public Const m_nTeamNum As Integer = &HF4
    Public Const m_nGlowIndex As Integer = &HA438
    Public Const m_fFlags As Integer = &H104
    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        'This gets called once we open the process
        'So it's a great place for us to initialize the cheat
        'And if succesfull, start the threads.

        'Try to find the process
        CSGOProc = Process.GetProcessesByName(szProcess)

        'Check if we found the process
        If CSGOProc.Length Then
            'Loop trough it's modules to find whatever we want
            For Each CurModule As System.Diagnostics.ProcessModule In CSGOProc(0).Modules
                'Check for "client.dll"
                If CurModule.ModuleName = "client.dll" Then
                    ClientDLL = CurModule.BaseAddress 'If found, store baseaddr
                End If 'We could exit here as we only need one module
            Next

            'If we got here and ClientDLL is still null
            'That means we failed to get the module baseaddr
            'No point continuing, inform user and abort
            If ClientDLL = 0 Then
                MsgBox("ClientDLL = 0!", MsgBoxStyle.Critical, "VBShit") 'Output the error
                Application.Exit() 'Quit
            End If

            'Start our function threads
            TriggerThread.Start()
            BhopThread.Start()
            'GlowThread.Start()

            MsgBox("Threads running!", MsgBoxStyle.Information, "VBShit") 'Inform user that threads are running, cheat is active
        Else
            MsgBox("Did not find the game!", MsgBoxStyle.Critical, "VBShit") 'Did not find the game, inform user about it
            Application.Exit() 'Quit
        End If
    End Sub
    Private Sub Form1_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        bShouldRun = False
        TriggerThread.Abort()
        BhopThread.Abort()
    End Sub
    Public Sub Triggerbot()
        While (bShouldRun)
            If GetAsyncKeyState(&H6) Then 'XButton2
                Dim pLocal As Integer = ReadInt(ClientDLL + m_dwLocalPlayer)
                If pLocal Then 'Check for null
                    Dim nCrossID As Integer = ReadInt(pLocal + m_nCrosshairID)

                    If nCrossID > 0 And nCrossID < 65 Then 'Default playerslots (Can go up to 128, hardcoding very bad)
                        Dim nLocalTeam As Integer = ReadInt(pLocal + m_nTeamNum)
                        Dim nCrossEnt As Integer = ReadInt(ClientDLL + m_dwEntityList + ((nCrossID - 1) * &H10))

                        If nCrossEnt Then 'Check for null
                            Dim nEntTeam As Integer = ReadInt(nCrossEnt + m_nTeamNum)

                            If Not nLocalTeam = nEntTeam Then 'Ignore team
                                mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 1)
                                Threading.Thread.Sleep(Int((20 * Rnd()) + 1)) 'Random delay before lift
                                mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 1)
                            End If
                        End If
                    End If
                End If
            End If

            Threading.Thread.Sleep(1)
        End While
    End Sub
    Public Sub BunnyHop() 'Requires you to unbind space and bind mouse3 (middle mouse) to +jump
        While (bShouldRun)
            If GetAsyncKeyState(&H20) Then 'Space
                Dim pLocal As Integer = ReadInt(ClientDLL + m_dwLocalPlayer)
                If pLocal Then 'Check for null

                    Dim nFlags As Integer = ReadInt(pLocal + m_fFlags)
                    If nFlags And FL_ONGROUND Then
                        mouse_event(MOUSEEVENTF_MIDDLEDOWN, 0, 0, 0, 1) 'Middle mouse down
                        Threading.Thread.Sleep(Int((20 * Rnd()) + 1)) 'Random delay before lift
                        mouse_event(MOUSEEVENTF_MIDDLEUP, 0, 0, 0, 1) 'Lift middle mouse
                    End If
                End If
            End If

            Threading.Thread.Sleep(1)
        End While
    End Sub
    Public Function ReadInt(ByVal nAddr As Integer) As Integer
        Dim nData As Integer
        ReadProcessMemory(CSGOProc(0).Handle, nAddr, nData, 4, 0)
        Return nData
    End Function
End Class