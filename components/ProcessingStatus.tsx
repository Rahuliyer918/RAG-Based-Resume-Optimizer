'use client';

import { useEffect, useState } from 'react';

const STEPS = [
  'Structuring resume...',
  'Generating embeddings...',
  'Searching relevant experience...',
  'Generating analysis...',
];

export default function ProcessingStatus() {
  const [currentStep, setCurrentStep] = useState(0);

  useEffect(() => {
    const interval = setInterval(() => {
      setCurrentStep((prev) => (prev + 1) % STEPS.length);
    }, 2500);
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="flex flex-col items-center justify-center min-h-[60vh] px-4">
      {/* Spinner */}
      <div className="relative w-20 h-20 mb-8">
        <div className="absolute inset-0 rounded-full border-4 border-slate-200" />
        <div className="absolute inset-0 rounded-full border-4 border-transparent border-t-indigo-600 animate-spin" />
        <div className="absolute inset-2 rounded-full border-4 border-transparent border-t-indigo-400 animate-spin [animation-duration:1.5s]" />
      </div>

      {/* Main text */}
      <h2 className="text-2xl font-semibold text-slate-800 mb-2">Analyzing your resume</h2>
      <p className="text-slate-500 mb-10 text-center max-w-sm">
        Our AI is working hard to match your experience to the job requirements
      </p>

      {/* Steps */}
      <div className="w-full max-w-xs space-y-3">
        {STEPS.map((step, index) => {
          const isActive = index === currentStep;
          const isDone = index < currentStep;
          return (
            <div
              key={step}
              className={`flex items-center gap-3 p-3 rounded-lg transition-all duration-500 ${
                isActive ? 'bg-indigo-50 border border-indigo-200' : 'opacity-40'
              }`}
            >
              <div
                className={`w-6 h-6 rounded-full flex items-center justify-center flex-shrink-0 transition-all ${
                  isDone
                    ? 'bg-green-500'
                    : isActive
                    ? 'bg-indigo-600 animate-pulse'
                    : 'bg-slate-200'
                }`}
              >
                {isDone ? (
                  <svg className="w-3.5 h-3.5 text-white" fill="none" stroke="currentColor" viewBox="0 0 24 24">
                    <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={3} d="M5 13l4 4L19 7" />
                  </svg>
                ) : isActive ? (
                  <div className="w-2 h-2 bg-white rounded-full" />
                ) : (
                  <div className="w-2 h-2 bg-slate-400 rounded-full" />
                )}
              </div>
              <span
                className={`text-sm font-medium ${
                  isActive ? 'text-indigo-700' : isDone ? 'text-green-700' : 'text-slate-500'
                }`}
              >
                {step}
              </span>
            </div>
          );
        })}
      </div>
    </div>
  );
}
