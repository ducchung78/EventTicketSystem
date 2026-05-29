// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// ===== Animated green light streaks background =====
(function () {
  'use strict';

  var canvas = document.getElementById('bg-streaks');
  if (!canvas) return;
  var ctx = canvas.getContext('2d');
  var TWO_PI = Math.PI * 2;
  var radialGrad = null;

  // yFrac: vertical position (0=top, 1=bottom)
  // amp: wave amplitude in px
  // period: ms per full drift cycle (8-15s)
  // waves: how many wave crests span the full width
  // phase: initial time offset for stagger
  // opacity: 0.15-0.45
  // lw: line width px (2-6)
  // blur: shadow blur px (10-30)
  var streaks = [
    { yFrac: 0.12, amp: 65, period: 12000, waves: 1.2, phase: 0.0, opacity: 0.35, lw: 2.5, blur: 18, color: '#00ff88' },
    { yFrac: 0.29, amp: 52, period:  9500, waves: 0.8, phase: 2.1, opacity: 0.25, lw: 4.0, blur: 26, color: '#00cc66' },
    { yFrac: 0.45, amp: 78, period: 14000, waves: 1.5, phase: 0.7, opacity: 0.20, lw: 2.0, blur: 14, color: '#00ff88' },
    { yFrac: 0.61, amp: 55, period: 11000, waves: 1.0, phase: 3.5, opacity: 0.38, lw: 3.5, blur: 22, color: '#00cc66' },
    { yFrac: 0.77, amp: 42, period: 13500, waves: 1.8, phase: 1.4, opacity: 0.18, lw: 5.0, blur: 30, color: '#00ff88' },
    { yFrac: 0.38, amp: 70, period:  8500, waves: 0.6, phase: 4.2, opacity: 0.28, lw: 3.0, blur: 20, color: '#00cc66' },
  ];

  function buildRadialGrad() {
    var W = canvas.width, H = canvas.height;
    var cx = W / 2, cy = H * 0.42;
    var r = Math.max(W, H) * 0.6;
    var g = ctx.createRadialGradient(cx, cy, 0, cx, cy, r);
    g.addColorStop(0,    'rgba(0, 50, 25, 0.10)');
    g.addColorStop(0.55, 'rgba(0,  0,  0, 0.00)');
    g.addColorStop(1,    'rgba(0,  0,  0, 0.28)');
    return g;
  }

  function resize() {
    canvas.width  = window.innerWidth;
    canvas.height = window.innerHeight;
    radialGrad = buildRadialGrad();
  }
  resize();
  window.addEventListener('resize', resize);

  var N = 90; // line-segment count per streak

  function draw(t) {
    var W = canvas.width, H = canvas.height;
    ctx.clearRect(0, 0, W, H);

    // Subtle radial focal point: slightly lighter center, darker vignette edges
    ctx.fillStyle = radialGrad;
    ctx.fillRect(0, 0, W, H);

    for (var i = 0; i < streaks.length; i++) {
      var s = streaks[i];
      var baseY = s.yFrac * H;
      var drift = TWO_PI * t / s.period; // phase advances with time → wave flows

      ctx.save();
      ctx.globalAlpha = s.opacity;
      ctx.strokeStyle = s.color;
      ctx.lineWidth   = s.lw;
      ctx.shadowColor = s.color;
      ctx.shadowBlur  = s.blur;
      ctx.lineCap     = 'round';
      ctx.lineJoin    = 'round';

      ctx.beginPath();
      for (var j = 0; j <= N; j++) {
        var x     = (j / N) * W;
        var phase = drift + s.phase + TWO_PI * s.waves * (j / N);
        var y     = baseY + s.amp * Math.sin(phase);
        if (j === 0) ctx.moveTo(x, y);
        else         ctx.lineTo(x, y);
      }
      ctx.stroke();
      ctx.restore();
    }
  }

  function animate(t) {
    draw(t);
    requestAnimationFrame(animate);
  }

  requestAnimationFrame(animate);
})();
