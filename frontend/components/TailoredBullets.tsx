'use client';

import { TailoredBullet } from '@/lib/types';

interface TailoredBulletsProps {
  bullets: TailoredBullet[];
}

export default function TailoredBullets({ bullets }: TailoredBulletsProps) {
  if (!bullets || bullets.length === 0) {
    return (
      <p className="text-sm text-slate-400 italic">No experience bullets available</p>
    );
  }

  return (
    <div className="overflow-x-auto -mx-1">
      <div className="min-w-full inline-block align-middle px-1">
        <div className="space-y-3">
          {bullets.map((bullet, index) => (
            <div
              key={index}
              className="border border-slate-200 rounded-xl overflow-hidden"
            >
              {/* Header: role/company */}
              {(bullet.role || bullet.company) && (
                <div className="flex items-center gap-2 px-4 py-2 bg-slate-50 border-b border-slate-200">
                  {bullet.role && (
                    <span className="text-xs font-semibold text-slate-700">{bullet.role}</span>
                  )}
                  {bullet.role && bullet.company && (
                    <span className="text-slate-300 text-xs">·</span>
                  )}
                  {bullet.company && (
                    <span className="text-xs text-slate-500">{bullet.company}</span>
                  )}
                </div>
              )}
              <div className="grid grid-cols-1 sm:grid-cols-2 divide-y sm:divide-y-0 sm:divide-x divide-slate-200">
                {/* Original */}
                <div className="px-4 py-3">
                  <p className="text-xs font-semibold text-slate-400 uppercase tracking-wide mb-1.5">Original</p>
                  <p className="text-sm text-slate-600 leading-relaxed">{bullet.original}</p>
                </div>
                {/* Tailored */}
                <div className="px-4 py-3 bg-indigo-50/40">
                  <p className="text-xs font-semibold text-indigo-500 uppercase tracking-wide mb-1.5">Tailored</p>
                  <p className="text-sm text-slate-800 leading-relaxed font-medium">{bullet.tailored}</p>
                </div>
              </div>
            </div>
          ))}
        </div>
      </div>
    </div>
  );
}
