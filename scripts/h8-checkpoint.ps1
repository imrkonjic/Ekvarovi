$ErrorActionPreference = "Stop"
$base = "http://localhost:5275/api"

function Login([string]$email) {
    $body = @{ email = $email; password = "Test123!" } | ConvertTo-Json
    $response = Invoke-RestMethod -Method Post -Uri "$base/auth/login" -ContentType "application/json" -Body $body
    return $response.token
}

function Call([string]$method, [string]$path, [string]$token, [object]$body = $null) {
    $headers = @{ Authorization = "Bearer $token" }
    $params = @{
        Method      = $method
        Uri         = "$base$path"
        Headers     = $headers
        ContentType = "application/json"
    }
    if ($null -ne $body) {
        $params.Body = ($body | ConvertTo-Json -Depth 5)
    }

    try {
        $response = Invoke-WebRequest @params -UseBasicParsing
        $parsed = $null
        if ($response.Content) {
            $parsed = $response.Content | ConvertFrom-Json
        }
        return [PSCustomObject]@{
            Status = [int]$response.StatusCode
            Body   = $parsed
            Raw    = $response.Content
        }
    }
    catch {
        $resp = $_.Exception.Response
        if ($null -eq $resp) { throw }

        $stream = $resp.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($stream)
        $content = $reader.ReadToEnd()
        $parsed = $null
        if ($content) {
            $parsed = $content | ConvertFrom-Json -ErrorAction SilentlyContinue
        }
        return [PSCustomObject]@{
            Status = [int]$resp.StatusCode
            Body   = $parsed
            Raw    = $content
        }
    }
}

Write-Host "=== H8 HAPPY PATH ==="
$reporter = Login "prijavitelj1@ekvarovi.hr"
$manager = Login "manager@ekvarovi.hr"
$tech1 = Login "tehnicar1@ekvarovi.hr"
$tech2 = Login "tehnicar2@ekvarovi.hr"

$locations = Invoke-RestMethod -Uri "$base/locations?page=1&pageSize=1" -Headers @{ Authorization = "Bearer $manager" }
$locationId = $locations.items[0].id

$create = Call "Post" "/faultreports" $reporter @{
    title       = "H8 test kvar"
    description = "Opis testne prijave za H8 kontrolnu tocku"
    locationId  = $locationId
}
Write-Host "1. Create report:" $create.Status $create.Body.reportNumber
$id = $create.Body.id

$triage = Call "Put" "/faultreports/$id/triage" $manager @{
    faultTypeId     = 1
    faultPriorityId = 2
    dueDate         = (Get-Date).AddDays(3).ToUniversalTime().ToString("o")
}
Write-Host "2. Triage:" $triage.Status $triage.Body.statusName

$tech1Id = (Invoke-RestMethod -Uri "$base/auth/me" -Headers @{ Authorization = "Bearer $tech1" }).id
$tech2Id = (Invoke-RestMethod -Uri "$base/auth/me" -Headers @{ Authorization = "Bearer $tech2" }).id

$assign = Call "Post" "/workassignments" $manager @{
    faultReportId    = $id
    technicianUserId = $tech1Id
    note             = "Prva dodjela"
}
Write-Host "3. Assign:" $assign.Status "assignmentId=$($assign.Body.id)"
$assignId = $assign.Body.id

$reassign = Call "Put" "/workassignments/$assignId/reassign" $manager @{
    newTechnicianUserId = $tech2Id
    reason              = "Promjena rasporeda H8"
    note                = "Re-dodjela"
}
Write-Host "4. Reassign:" $reassign.Status "newAssignmentId=$($reassign.Body.id)"
$newAssignId = $reassign.Body.id

$intervention = Call "Post" "/interventions" $tech2 @{
    workAssignmentId = $newAssignId
    note             = "Pocetak rada"
}
Write-Host "5. Start intervention:" $intervention.Status "interventionId=$($intervention.Body.id)"
$intId = $intervention.Body.id

$fail = Call "Put" "/interventions/$intId/finish" $tech2 @{
    isSuccessful  = $false
    note          = "Neuspjesan pokusaj popravka opreme"
    failureReason = "Nedostaje rezervni dio"
}
Write-Host "6. Fail intervention:" $fail.Status $fail.Body.statusName

$intervention2 = Call "Post" "/interventions" $tech2 @{
    workAssignmentId = $newAssignId
    note             = "Drugi pokusaj"
}
Write-Host "7. Start 2nd intervention:" $intervention2.Status "interventionId=$($intervention2.Body.id)"
$intId2 = $intervention2.Body.id

$success = Call "Put" "/interventions/$intId2/finish" $tech2 @{
    isSuccessful = $true
    note         = "Uspjesno rijesen kvar nakon zamjene dijela"
}
Write-Host "8. Success intervention:" $success.Status $success.Body.faultStatusName

$close = Call "Put" "/faultreports/$id/close" $manager @{
    closingNote = "Provjera zavrsena, rad prihvacen"
}
Write-Host "9. Close report:" $close.Status $close.Body.statusName

Write-Host ""
Write-Host "=== H8 RULE VIOLATIONS ==="

$v1 = Call "Put" "/faultreports/$id/close" $tech2 @{ closingNote = "Pokusaj zatvaranja" }
Write-Host "V1 Technician close (403):" $v1.Status

$create2 = Call "Post" "/faultreports" $reporter @{
    title       = "H8 BR25 test"
    description = "Druga prijava za test pravila BR25"
    locationId  = $locationId
}
$id2 = $create2.Body.id
Call "Put" "/faultreports/$id2/triage" $manager @{
    faultTypeId = 1; faultPriorityId = 2; dueDate = (Get-Date).AddDays(2).ToUniversalTime().ToString("o")
} | Out-Null
$asg2 = Call "Post" "/workassignments" $manager @{ faultReportId = $id2; technicianUserId = $tech2Id; note = "Dodjela" }
$asg2Id = $asg2.Body.id
Call "Post" "/interventions" $tech2 @{ workAssignmentId = $asg2Id; note = "Prva" } | Out-Null
$v3 = Call "Post" "/interventions" $tech2 @{ workAssignmentId = $asg2Id; note = "Druga u tijeku" }
Write-Host "V3 Second running intervention BR-25 (409):" $v3.Status

$create3 = Call "Post" "/faultreports" $reporter @{
    title       = "H8 BR14 test"
    description = "Treca prijava za test zatvaranja BR14"
    locationId  = $locationId
}
$id3 = $create3.Body.id
Call "Put" "/faultreports/$id3/triage" $manager @{
    faultTypeId = 1; faultPriorityId = 2; dueDate = (Get-Date).AddDays(2).ToUniversalTime().ToString("o")
} | Out-Null
$asg3 = Call "Post" "/workassignments" $manager @{ faultReportId = $id3; technicianUserId = $tech1Id; note = "Dodjela" }
$asg3Id = $asg3.Body.id
$intB = Call "Post" "/interventions" $tech1 @{ workAssignmentId = $asg3Id; note = "Rad" }
$intBId = $intB.Body.id
Call "Put" "/interventions/$intBId/finish" $tech1 @{
    isSuccessful = $true
    note         = "Uspjesno obavljen rad na prijavi"
} | Out-Null
$v4 = Call "Put" "/faultreports/$id3/close" $manager @{ closingNote = "kr" }
Write-Host "V4 Short closing note BR-14 (400):" $v4.Status

$v5 = Call "Put" "/interventions/$intBId/finish" $tech2 @{
    isSuccessful = $true
    note         = "Pokusaj tudjeg zavrsetka intervencije"
}
Write-Host "V5 Wrong technician finish BR-33 (403):" $v5.Status

$create4 = Call "Post" "/faultreports" $reporter @{
    title       = "H8 BR13 test"
    description = "Cetvrta prijava bez uspjesne intervencije"
    locationId  = $locationId
}
$id4 = $create4.Body.id
Call "Put" "/faultreports/$id4/triage" $manager @{
    faultTypeId = 1; faultPriorityId = 2; dueDate = (Get-Date).AddDays(2).ToUniversalTime().ToString("o")
} | Out-Null
$asg4 = Call "Post" "/workassignments" $manager @{ faultReportId = $id4; technicianUserId = $tech1Id; note = "Dodjela" }
$asg4Id = $asg4.Body.id
$intC = Call "Post" "/interventions" $tech1 @{ workAssignmentId = $asg4Id; note = "Rad" }
$intCId = $intC.Body.id
Call "Put" "/interventions/$intCId/finish" $tech1 @{
    isSuccessful  = $false
    note          = "Neuspjesan rad bez rijesenja"
    failureReason = "Nema dijela"
} | Out-Null
$v6 = Call "Put" "/faultreports/$id4/close" $manager @{ closingNote = "Pokusaj zatvaranja bez uspjeha" }
Write-Host "V6 Close without success BR-13/09 (400):" $v6.Status

$passed = @(
    ($create.Status -eq 201 -or $create.Status -eq 200)
    ($close.Status -eq 200)
    ($v1.Status -eq 403)
    ($v3.Status -eq 409)
    ($v4.Status -eq 400)
    ($v5.Status -eq 403)
    ($v6.Status -eq 400)
)
if ($passed -contains $false) {
    Write-Host "H8 CHECKPOINT FAILED"
    exit 1
}
Write-Host "H8 CHECKPOINT PASSED"
