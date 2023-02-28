📂 DICOM ORGANIZE
--------------
[![Build Status](https://img.shields.io/endpoint.svg?url=https%3A%2F%2Factions-badge.atrox.dev%2Famoerie%2Fdcmorganize%2Fbadge%3Fref%3Dmain&style=for-the-badge&label=Build)](https://actions-badge.atrox.dev/amoerie/dcmorganize/goto?ref=main) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/DcmOrganize?label=DcmOrganize&style=for-the-badge)](https://www.nuget.org/packages/DcmOrganize)

Organizes DICOM files on your disk.

Installation
------------

```powershell
    dotnet tool install dcmorganize --global
```

Usage
-----

```powershell
    dcmorganize file1.dcm file2.dcm file3.dcm
```

or using piped input

```powershell
    dcmfind | dcmorganize
```

See https://github.com/amoerie/dcmfind

Parameters 

```
  files pos. 0               Required. (Default: .) Organize these DICOM files. When missing, this option will be read from the piped input.
  
  -t, --targetDirectory      (Default: .) Organize DICOM files in this directory

  -f, --targetFilePattern    (Default: {PatientName}/{AccessionNumber}/{SeriesNumber}/{InstanceNumber ?? SOPInstanceUID} - {Guid}.dcm) Write DICOM files using this pattern. DICOM tags are supported. Fallback
                             for missing DICOM tags are supported. Nested directories will be created on demand.

  -a, --targetFileAction     (Default: Move) Action to execute for each file

  -p, --parallelism          (Default: 8) Process this many files in parallel

  --help                     Display this help screen.

  --version                  Display version information.
```
