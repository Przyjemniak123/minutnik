using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Threading;

namespace minutnik;

public partial class MainWindow : Window
{
    private const int DefaultWorkDurationMinutes = 25;
    private const int DefaultBreakDurationMinutes = 5;

    private readonly DispatcherTimer _timer;
    private DateTimeOffset _deadline;
    private int _remainingSeconds = DefaultWorkDurationMinutes * 60;
    private int _completedCycles;
    private bool _isBreak;
    private bool _isRunning;
    private bool _hasStarted;
    private readonly List<string> _history = new();

    public MainWindow()
    {
        InitializeComponent();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += OnTimerTick;
        WorkDurationInput.ValueChanged += (_, _) =>
        {
            if (!_hasStarted && !_isRunning && !_isBreak)
            {
                _remainingSeconds = GetWorkDurationSeconds();
                UpdateDisplay();
            }
        };
        UpdateDisplay();
    }

    private void OnStartPauseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (_isRunning)
        {
            AdvanceTimer(DateTimeOffset.UtcNow);
            _remainingSeconds = GetRemainingSeconds(DateTimeOffset.UtcNow);
            _timer.Stop();
            _isRunning = false;
        }
        else
        {
            _deadline = DateTimeOffset.UtcNow.AddSeconds(_remainingSeconds);
            _timer.Start();
            _isRunning = true;
            _hasStarted = true;
        }

        UpdateDisplay();
    }

    private void OnResetClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        _timer.Stop();
        _isRunning = false;
        _hasStarted = false;
        _isBreak = false;
        _completedCycles = 0;
        _remainingSeconds = GetWorkDurationSeconds();
        UpdateDisplay();
    }

    private void OnTimerTick(object? sender, EventArgs e)
    {
        AdvanceTimer(DateTimeOffset.UtcNow);
        UpdateDisplay();
    }

    private void AdvanceTimer(DateTimeOffset now)
    {
        while (_isRunning && now >= _deadline)
        {
            if (_isBreak)
            {
                _completedCycles++;
                _history.Add($"{DateTime.Now:HH:mm} — cykl {_completedCycles}");
                HistoryList.ItemsSource = _history.ToArray();
            }
            

            _isBreak = !_isBreak;
            _remainingSeconds = _isBreak ? GetBreakDurationSeconds() : GetWorkDurationSeconds();
            _deadline = _deadline.AddSeconds(_remainingSeconds);
        }
    }

    private int GetRemainingSeconds(DateTimeOffset now)
    {
        return Math.Max(0, (int)Math.Ceiling((_deadline - now).TotalSeconds));
    }

    private int GetWorkDurationSeconds()
    {
        return (int)(WorkDurationInput.Value ?? DefaultWorkDurationMinutes) * 60;
    }

    private int GetBreakDurationSeconds()
    {
        return (int)(BreakDurationInput.Value ?? DefaultBreakDurationMinutes) * 60;
    }

    private void UpdateDisplay()
    {
        var seconds = _isRunning ? GetRemainingSeconds(DateTimeOffset.UtcNow) : _remainingSeconds;
        TimerText.Text = $"{seconds / 60:00}:{seconds % 60:00}";
        PhaseText.Text = _isBreak ? "PRZERWA" : "PRACA";
        CycleCountText.Text = _completedCycles.ToString();
        StartPauseButton.Content = _isRunning ? "Pauza" : _hasStarted ? "Wznów" : "Start";

        StatusText.Text = _isRunning
            ? _isBreak ? "Czas na odpoczynek" : "Czas na skupienie"
            : _hasStarted ? "Minutnik wstrzymany" : "Gotowy do pracy";
    }
}