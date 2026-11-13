'use client';

interface MatchScoreGaugeProps {
  score: number;
}

export default function MatchScoreGauge({ score }: MatchScoreGaugeProps) {
  const clampedScore = Math.min(100, Math.max(0, score));

  // SVG arc gauge parameters
  const size = 140;
  const cx = size / 2;
  const cy = size / 2;
  const radius = 54;
  const strokeWidth = 12;

  // Arc spans 240 degrees (from 150deg to 390deg / -210deg to 30deg)
  const startAngleDeg = 150;
  const totalArcDeg = 240;
  const circumference = 2 * Math.PI * radius;
  const arcLength = (totalArcDeg / 360) * circumference;

  // Progress arc
  const progressLength = (clampedScore / 100) * arcLength;
  const gapLength = circumference - progressLength;

  // Convert degree to radians for path calculation
  const toRad = (deg: number) => (deg * Math.PI) / 180;

  const startRad = toRad(startAngleDeg);
  const endRad = toRad(startAngleDeg + totalArcDeg);

  const startX = cx + radius * Math.cos(startRad);
  const startY = cy + radius * Math.sin(startRad);
  const endX = cx + radius * Math.cos(endRad);
  const endY = cy + radius * Math.sin(endRad);

  // Background arc path
  const bgArcPath = `M ${startX} ${startY} A ${radius} ${radius} 0 1 1 ${endX} ${endY}`;

  // Score color
  const color =
    clampedScore >= 70 ? '#22c55e' : clampedScore >= 50 ? '#f59e0b' : '#ef4444';
  const bgColor =
    clampedScore >= 70 ? '#dcfce7' : clampedScore >= 50 ? '#fef3c7' : '#fee2e2';
  const textColor =
    clampedScore >= 70 ? 'text-green-600' : clampedScore >= 50 ? 'text-amber-600' : 'text-red-600';
  const label =
    clampedScore >= 70 ? 'Strong Match' : clampedScore >= 50 ? 'Partial Match' : 'Low Match';

  // Stroke dash offset calculation for progress arc
  // We use a single circle path but manipulate dasharray/offset
  const dashArray = `${progressLength} ${circumference - progressLength}`;
  // The offset needs to position the start of the dash at our start angle
  // CSS stroke-dashoffset shifts backward from "3 o'clock" position (0deg)
  // Our start is at 150deg, so offset = circumference - (150/360 * circumference)
  const dashOffset = circumference - (startAngleDeg / 360) * circumference;

  return (
    <div className="flex flex-col items-center">
      <div className="relative" style={{ width: size, height: size }}>
        <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`}>
          {/* Background track */}
          <circle
            cx={cx}
            cy={cy}
            r={radius}
            fill="none"
            stroke="#e2e8f0"
            strokeWidth={strokeWidth}
            strokeLinecap="round"
            strokeDasharray={`${arcLength} ${circumference - arcLength}`}
            strokeDashoffset={dashOffset}
            transform={`rotate(0 ${cx} ${cy})`}
          />
          {/* Progress arc */}
          <circle
            cx={cx}
            cy={cy}
            r={radius}
            fill="none"
            stroke={color}
            strokeWidth={strokeWidth}
            strokeLinecap="round"
            strokeDasharray={dashArray}
            strokeDashoffset={dashOffset}
            style={{ transition: 'stroke-dasharray 1s ease-in-out' }}
          />
        </svg>
        {/* Score text in center */}
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <span className={`text-3xl font-bold ${textColor}`}>{clampedScore}</span>
          <span className="text-xs text-slate-400 font-medium">/ 100</span>
        </div>
      </div>
      <div
        className="mt-2 px-3 py-1 rounded-full text-xs font-semibold"
        style={{ backgroundColor: bgColor, color: color }}
      >
        {label}
      </div>
    </div>
  );
}
