# Run-TestCases.ps1 - Automated test runner for the RAG Resume Optimizer API
# Usage: .\tests\Run-TestCases.ps1
# Requires: API running at http://localhost:5272

$API           = "http://localhost:5272"
$POLL_INTERVAL = 5    # seconds between status checks
$MAX_WAIT      = 180  # seconds before giving up

# ---------------------------------------------------------------------------
# Resume (shared across all test cases)
# ---------------------------------------------------------------------------
$RESUME = @"
Rahulnarayan Iyer
New York, New York | rahuliyer918@gmail.com

SUMMARY
Software Engineer with 3+ years of production experience designing and operating distributed backend systems at scale. Proven track record diagnosing production failures, reducing transaction failure rates from 5% to 0.2%, and improving system throughput by 40% across high-volume transactional workloads. Strong foundation in fault isolation, idempotent transaction design, and distributed tracing across multi-service architectures.

SKILLS
Core Backend: C#, .NET, ASP.NET Core, RESTful APIs, SQL Server optimization, Transaction management, Idempotency
Cloud and DevOps: Azure (App Services, Azure DevOps), Docker, CI/CD pipelines
Supporting Technologies: Python, JavaScript, TypeScript, MongoDB, C++

PROFESSIONAL EXPERIENCE

Software Engineer - Binghamton University (Jan 2024 - Present)
- Engineered an AutoIt automation system for end-to-end FaceReader 10.0 ingestion and post-analysis output generation, enabling the pipeline to scale to thousands of videos without manual intervention.
- Built a Python pipeline using a custom YouTube Data API client to programmatically source interview recordings across hundreds of political figures for downstream facial analysis research.
- Cut dataset preparation time by 90% by designing automated data sourcing and ingestion workflows for an NLP-driven interview quality study.

Backend Engineer - Capgemini Technology Services (Feb 2021 - July 2023)
- Diagnosed and resolved cascading failures across a multi-service payment flow for a Fortune 500 QSR chain with 25,000+ US locations, cutting transaction failure rates from 5% to 0.2% by instrumenting distributed traces and isolating fault boundaries.
- Increased system throughput by 40% by rewriting N+1 SQL queries into batched set-based operations and offloading synchronous payment callbacks to async processing queues, reducing average API response time from 800ms to 480ms.
- Eliminated duplicate transaction processing errors by designing idempotent payment handlers with database-level consistency guarantees.
- Improved production debuggability by implementing distributed tracing and structured log correlation across interdependent payment services.
- Reduced deployment risk by authoring an automated regression and smoke test suite covering critical payment workflows, integrated directly into the CI pipeline.
- Managed production deployments and change management across a 17-application enterprise portfolio at a large-scale energy sector enterprise.

PROJECTS
RAG-Powered Resume Optimizer | C#, ASP.NET Core 8, MongoDB Atlas, Google Gemini API
- Engineered a distributed REST API for automated resume tailoring integrating ASP.NET Core 8 with MongoDB Atlas Vector Search and Google Gemini.
- Built a PDF parsing and chunking pipeline storing vectorized skill segments using the Gemini Embedding API.

Social Media Sentiment Data Pipeline | Python, MongoDB, Flask, Plotly
- Built a multi-threaded ingestion pipeline collecting 5.8M+ posts from Reddit and 4chan.
- Integrated the ModerateHateSpeech API to score toxicity across 1.1M+ Reddit comments.
- Developed an interactive Flask dashboard with Plotly visualizations.

EDUCATION
Binghamton University - Master of Science in Computer Science (Aug 2023 - May 2025)
Savitribai Phule Pune University - Bachelor of Engineering in Electronics and Telecommunication

CERTIFICATIONS
Microsoft Certified: Azure Administrator Associate
"@

# ---------------------------------------------------------------------------
# Job Descriptions
# ---------------------------------------------------------------------------
$JD_STRONG = @"
Senior Backend Engineer - FinTech Platform (C#/.NET)

We are looking for a Senior Backend Engineer to join our core payments infrastructure team building systems that process millions of transactions daily.

Required Qualifications:
- 3+ years of backend engineering experience
- Expert-level C# and .NET (ASP.NET Core, RESTful APIs)
- Distributed systems design: fault isolation, idempotency, retry patterns
- SQL Server: query optimization, indexing, transaction management
- Azure cloud services (App Services, Service Bus, Azure DevOps)
- CI/CD pipelines and DevOps practices

Preferred Qualifications:
- Experience with high-volume payment or financial transaction systems
- Docker and container orchestration
- Distributed tracing and structured logging (Serilog, OpenTelemetry)
- MongoDB or other NoSQL databases
"@

$JD_WEAK = @"
iOS Mobile Developer

We are seeking an experienced iOS Developer to join our mobile team building consumer-facing applications.

Required Qualifications:
- 2+ years of native iOS development experience
- Expert proficiency in Swift and Objective-C
- Deep knowledge of UIKit, SwiftUI, and the iOS SDK
- Experience with Xcode, Instruments, and XCTest
- Understanding of iOS memory management and performance optimization
- App Store submission and TestFlight experience
- Core Data or Realm for local persistence

Preferred Qualifications:
- Experience with ARKit or CoreML
- Familiarity with React Native or Flutter
- Push notifications and background processing
"@

$JD_PARTIAL = @"
Full Stack Software Engineer (.NET + React)

We are building a modern SaaS platform and need a Full Stack Engineer comfortable across the entire stack.

Required Qualifications:
- 2+ years of full stack development experience
- Strong C# and ASP.NET Core for backend REST APIs
- React (v18+) with TypeScript for frontend development
- State management with Redux or Zustand
- SQL Server or PostgreSQL for relational data
- Git version control and code review practices
- Understanding of responsive design and CSS fundamentals

Preferred Qualifications:
- Azure or AWS cloud experience
- Docker and CI/CD pipelines
- Testing: xUnit for backend, Jest and React Testing Library for frontend
"@

# ---------------------------------------------------------------------------
# Helper: submit one analysis, return the analysisId
# ---------------------------------------------------------------------------
function Submit-Analysis {
    param(
        [string]$ResumeText,
        [string[]]$JobDescriptions,
        [string]$Label
    )

    Write-Host ""
    Write-Host "[$Label] Submitting..." -ForegroundColor Cyan

    $boundary = [System.Guid]::NewGuid().ToString("N")
    $sb = New-Object System.Text.StringBuilder

    # Add resumeText field
    $null = $sb.Append("--$boundary`r`n")
    $null = $sb.Append("Content-Disposition: form-data; name=`"resumeText`"`r`n`r`n")
    $null = $sb.Append("$ResumeText`r`n")

    # Add style field
    $null = $sb.Append("--$boundary`r`n")
    $null = $sb.Append("Content-Disposition: form-data; name=`"style`"`r`n`r`n")
    $null = $sb.Append("metrics-focused`r`n")

    # Add each JD as a separate jobDescriptions field
    foreach ($jd in $JobDescriptions) {
        $null = $sb.Append("--$boundary`r`n")
        $null = $sb.Append("Content-Disposition: form-data; name=`"jobDescriptions`"`r`n`r`n")
        $null = $sb.Append("$jd`r`n")
    }

    $null = $sb.Append("--$boundary--`r`n")

    $bodyBytes = [System.Text.Encoding]::UTF8.GetBytes($sb.ToString())

    try {
        $response = Invoke-RestMethod `
            -Uri "$API/api/analyze" `
            -Method POST `
            -ContentType "multipart/form-data; boundary=$boundary" `
            -Body $bodyBytes

        Write-Host "[$Label] Accepted - ID: $($response.analysisId)" -ForegroundColor Green
        return $response.analysisId
    }
    catch {
        Write-Host "[$Label] Submit FAILED: $($_.Exception.Message)" -ForegroundColor Red
        return $null
    }
}

# ---------------------------------------------------------------------------
# Helper: poll until complete/failed/timeout, return result object
# ---------------------------------------------------------------------------
function Wait-ForResult {
    param([string]$Id, [string]$Label)

    if (-not $Id) { return $null }

    $elapsed = 0
    while ($elapsed -lt $MAX_WAIT) {
        Start-Sleep -Seconds $POLL_INTERVAL
        $elapsed += $POLL_INTERVAL

        try {
            $result = Invoke-RestMethod -Uri "$API/api/analyze/$Id" -Method GET

            if ($result.status -eq "complete") {
                Write-Host "[$Label] Done in $($elapsed)s" -ForegroundColor Green
                return $result
            }
            elseif ($result.status -eq "failed") {
                Write-Host "[$Label] FAILED: $($result.error)" -ForegroundColor Red
                return $result
            }
            else {
                Write-Host "[$Label] Processing... ($($elapsed)s elapsed)" -ForegroundColor Yellow
            }
        }
        catch {
            Write-Host "[$Label] Poll error: $($_.Exception.Message)" -ForegroundColor Red
        }
    }

    Write-Host "[$Label] Timed out after $($MAX_WAIT)s" -ForegroundColor Red
    return $null
}

# ---------------------------------------------------------------------------
# Helper: print one result's summary
# ---------------------------------------------------------------------------
function Print-Result {
    param($Result, [string]$Label, [string]$ExpectedRange)

    if (-not $Result) {
        Write-Host "  [$Label] No result (timeout or submit failed)" -ForegroundColor Red
        return
    }
    if ($Result.status -eq "failed") {
        Write-Host "  [$Label] Pipeline failed: $($Result.error)" -ForegroundColor Red
        return
    }

    foreach ($r in $Result.results) {
        $score    = [math]::Round($r.matchScore, 1)
        $jdNum    = $r.jobDescriptionIndex + 1
        $aligned  = if ($r.alignedSkills.Count -gt 0) { $r.alignedSkills -join ", " } else { "(none)" }
        $gaps     = if ($r.trueGaps.Count -gt 0) { $r.trueGaps -join ", " } else { "(none)" }
        $under    = if ($r.underrepresentedSkills.Count -gt 0) {
                        ($r.underrepresentedSkills | ForEach-Object { $_.skill }) -join ", "
                    } else { "(none)" }

        $scoreColor = if ($score -ge 70) { "Green" } elseif ($score -ge 45) { "Yellow" } else { "Red" }

        Write-Host ""
        Write-Host "  [$Label - JD $jdNum]" -ForegroundColor White
        Write-Host ("  Score:            ") -NoNewline
        Write-Host $score -ForegroundColor $scoreColor -NoNewline
        Write-Host " / 100  (expected: $ExpectedRange)"
        Write-Host "  Aligned Skills:   $aligned"
        Write-Host "  True Gaps:        $gaps"
        Write-Host "  Underrepresented: $under"
    }
}

# ---------------------------------------------------------------------------
# Main - submit all test cases, then poll sequentially
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==========================================" -ForegroundColor Magenta
Write-Host "  RAG Resume Optimizer - Test Suite" -ForegroundColor Magenta
Write-Host "==========================================" -ForegroundColor Magenta
Write-Host "  4 test cases: Strong / Weak / Partial / Multi-JD"
Write-Host "  NOTE: Running sequentially with cooldown to respect free-tier rate limit (5 req/min)"
Write-Host ""

$COOLDOWN = 70  # seconds to wait between submissions (each pipeline uses 2 generateContent calls)

# Submit and fully complete each test case before starting the next
# to stay within the 5 req/min free-tier quota for generateContent

$id1 = Submit-Analysis -ResumeText $RESUME -JobDescriptions @($JD_STRONG) -Label "TC1 Strong (.NET Backend)"
$r1  = Wait-ForResult  -Id $id1 -Label "TC1 Strong"

Write-Host ""
Write-Host "Cooling down $($COOLDOWN)s before next submission (free-tier rate limit)..." -ForegroundColor DarkGray
Start-Sleep -Seconds $COOLDOWN

$id2 = Submit-Analysis -ResumeText $RESUME -JobDescriptions @($JD_WEAK) -Label "TC2 Weak (iOS Dev)"
$r2  = Wait-ForResult  -Id $id2 -Label "TC2 Weak"

Write-Host ""
Write-Host "Cooling down $($COOLDOWN)s before next submission..." -ForegroundColor DarkGray
Start-Sleep -Seconds $COOLDOWN

$id3 = Submit-Analysis -ResumeText $RESUME -JobDescriptions @($JD_PARTIAL) -Label "TC3 Partial (Full Stack)"
$r3  = Wait-ForResult  -Id $id3 -Label "TC3 Partial"

Write-Host ""
Write-Host "Cooling down $($COOLDOWN)s before next submission..." -ForegroundColor DarkGray
Start-Sleep -Seconds $COOLDOWN

$id4 = Submit-Analysis -ResumeText $RESUME -JobDescriptions @($JD_STRONG,$JD_WEAK) -Label "TC4 Multi-JD"
$r4  = Wait-ForResult  -Id $id4 -Label "TC4 Multi-JD"

# ---------------------------------------------------------------------------
# Results summary
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "==========================================" -ForegroundColor Magenta
Write-Host "  RESULTS SUMMARY" -ForegroundColor Magenta
Write-Host "==========================================" -ForegroundColor Magenta

Print-Result -Result $r1 -Label "TC1 Strong (.NET Backend)" -ExpectedRange "85-95"
Print-Result -Result $r2 -Label "TC2 Weak (iOS Dev)"        -ExpectedRange "10-20"
Print-Result -Result $r3 -Label "TC3 Partial (Full Stack)"  -ExpectedRange "50-65"
Print-Result -Result $r4 -Label "TC4 Multi-JD"              -ExpectedRange "JD1:85-95 / JD2:10-20"

Write-Host ""
Write-Host "==========================================" -ForegroundColor Magenta
Write-Host "  Done." -ForegroundColor Green
Write-Host "==========================================" -ForegroundColor Magenta
