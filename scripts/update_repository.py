"""Import built Dalamud ZIPs and regenerate a shared custom repository manifest."""
import argparse
import json
import re
import shutil
import zipfile
from pathlib import Path


def read_package(archive):
    with zipfile.ZipFile(archive) as package:
        if package.testzip() is not None:
            raise ValueError(f'Corrupt package: {archive}')
        candidates = [n for n in package.namelist() if '/' not in n and n.endswith('.json') and not n.endswith('.deps.json')]
        manifests = [json.loads(package.read(n)) for n in candidates]
        manifests = [m for m in manifests if 'InternalName' in m and 'AssemblyVersion' in m]
        if len(manifests) != 1:
            raise ValueError(f'Expected one plugin manifest: {archive}')
        manifest = manifests[0]
        name = manifest['InternalName']
        if not re.fullmatch(r'[A-Za-z][A-Za-z0-9_]*', name):
            raise ValueError('Invalid internal plugin name')
        version = manifest['AssemblyVersion']
        if not re.fullmatch(r'\d+\.\d+\.\d+\.\d+', version):
            raise ValueError('Expected a four-part plugin version')
        if f'{name}.dll' not in package.namelist():
            raise ValueError('Plugin DLL is missing')
        if not isinstance(manifest.get('DalamudApiLevel'), int):
            raise ValueError('Dalamud API level is missing')
        return manifest


def version_key(manifest):
    return tuple(map(int, manifest['AssemblyVersion'].split('.')))


def update(root, archives, repository, branch):
    # Validate all input before importing files or changing the index.
    inputs = [(Path(p), read_package(p)) for p in archives]
    for archive, manifest in inputs:
        destination = root / 'plugins' / manifest['InternalName'] / manifest['AssemblyVersion'] / 'latest.zip'
        if destination.exists() and destination.read_bytes() != archive.read_bytes():
            raise ValueError(f'Bump the version before replacing {destination}')
    for archive, manifest in inputs:
        destination = root / 'plugins' / manifest['InternalName'] / manifest['AssemblyVersion'] / 'latest.zip'
        destination.parent.mkdir(parents=True, exist_ok=True)
        if not destination.exists():
            shutil.copy2(archive, destination)

    latest = {}
    for archive in sorted((root / 'plugins').glob('*/*/latest.zip')):
        manifest = read_package(archive)
        expected = root / 'plugins' / manifest['InternalName'] / manifest['AssemblyVersion'] / 'latest.zip'
        if archive != expected:
            raise ValueError(f'Package path does not match its manifest: {archive}')
        name = manifest['InternalName']
        if name not in latest or version_key(manifest) > version_key(latest[name][0]):
            latest[name] = manifest, archive
    result = []
    for name, (manifest, archive) in sorted(latest.items()):
        url = f'https://raw.githubusercontent.com/{repository}/{branch}/{archive.relative_to(root).as_posix()}'
        manifest.update(DownloadLinkInstall=url, DownloadLinkUpdate=url, DownloadLinkTesting=url,
                        IsHide=False, IsTestingExclusive=False, LastUpdate=str(int(archive.stat().st_mtime)))
        if not manifest.get('RepoUrl'):
            manifest['RepoUrl'] = f'https://github.com/{repository}'
        result.append(manifest)
    index = root / 'pluginmaster.json'
    temporary = root / 'pluginmaster.json.tmp'
    temporary.write_text(json.dumps(result, indent=2) + '\n', encoding='utf-8')
    temporary.replace(index)
    return result


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('archives', nargs='*', type=Path, help='Built latest.zip files to import')
    parser.add_argument('--repository', default='Isaiahfeldt/ffxiv')
    parser.add_argument('--branch', default='main')
    args = parser.parse_args()
    if not re.fullmatch(r'[A-Za-z0-9_.-]+/[A-Za-z0-9_.-]+', args.repository):
        parser.error('Expected owner/repository')
    result = update(Path(__file__).resolve().parents[1], args.archives, args.repository, args.branch)
    for manifest in result:
        print(f"{manifest['Name']} {manifest['AssemblyVersion']}: {manifest['DownloadLinkInstall']}")
