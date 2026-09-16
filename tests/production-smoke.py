"""Exercise the built image against disposable PostgreSQL, without external email."""
import json, os, secrets, subprocess, time, urllib.request

def docker(*args, **kwargs):
    return subprocess.check_output(['docker', *args], text=True, **kwargs).strip()

opener = urllib.request.build_opener(urllib.request.ProxyHandler({}))
def ready():
    for _ in range(90):
        try:
            with opener.open('http://127.0.0.1:8082/health', timeout=2) as r:
                if r.status == 200:
                    return
        except Exception:
            time.sleep(1)
    raise RuntimeError('Production container did not become healthy')

password = secrets.token_hex(24)
if os.environ.get('GITHUB_ACTIONS'):
    print('::add-mask::' + password, flush=True)
env = {**os.environ, 'POSTGRES_PASSWORD': password,
       'ConnectionStrings__Default': f'Host=talume-ci-db;Database=postgres;Username=postgres;Password={password}'}
try:
    docker('network', 'create', 'talume-ci')
    docker('volume', 'create', 'talume-ci-keys')
    docker('run', '-d', '--name', 'talume-ci-db', '--network', 'talume-ci',
           '-e', 'POSTGRES_PASSWORD', 'postgres:18', env=env)
    for _ in range(60):
        if subprocess.run(['docker', 'exec', 'talume-ci-db', 'pg_isready', '-U', 'postgres'],
                          stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL).returncode == 0:
            break
        time.sleep(1)
    else:
        raise RuntimeError('PostgreSQL did not become ready')
    docker('run', '-d', '--name', 'talume-ci-app', '--network', 'talume-ci',
           '-p', '127.0.0.1:8082:8080', '-v', 'talume-ci-keys:/app/keys',
           '-e', 'ASPNETCORE_ENVIRONMENT=Production', '-e', 'ConnectionStrings__Default',
           '-e', 'Database__Initialize=true', '-e', 'Demo__Seed=false',
           '-e', 'Mail__Provider=Resend', '-e', 'Registration__AllowPublicFreelancers=false',
           'talume-validation', env=env)
    ready()
    with opener.open('http://127.0.0.1:8082/api/auth/config') as r:
        assert json.load(r)['demo'] is False, 'Production must disable demo mode'
    with opener.open('http://127.0.0.1:8082/Account') as r:
        assert r.status == 200 and 'csrf-token' in r.read().decode()
    status = docker('exec', 'talume-ci-app', 'cat', '/proc/1/status')
    uid = next(line.split()[1] for line in status.splitlines() if line.startswith('Uid:'))
    assert uid != '0', 'Application process must not run as root'
    before = docker('exec', 'talume-ci-app', 'sh', '-c', 'sha256sum /app/keys/*.xml')
    assert before, 'Data Protection keys must be created in the volume'
    docker('restart', 'talume-ci-app')
    ready()
    after = docker('exec', 'talume-ci-app', 'sh', '-c', 'sha256sum /app/keys/*.xml')
    assert before == after, 'Data Protection keys must persist across restarts'
    print('PASS: production image, PostgreSQL bootstrap, health, account page, demo disabled, non-root process and persistent keys.')
except Exception:
    subprocess.run(['docker', 'logs', '--tail', '60', 'talume-ci-app'])
    raise
finally:
    subprocess.run(['docker', 'rm', '-f', 'talume-ci-app', 'talume-ci-db'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(['docker', 'volume', 'rm', 'talume-ci-keys'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(['docker', 'network', 'rm', 'talume-ci'], stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
