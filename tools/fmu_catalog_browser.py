#!/usr/bin/env python3
"""
FMU Catalog Browser v3 — Uses Contents API (no tree truncation).
Only fetches CoSimulation + win64 FMUs (PowerSimBuilder-compatible).

Usage:
  python tools/fmu_catalog_browser.py
"""

from __future__ import annotations
import json, os, threading, urllib.request, urllib.error, zipfile, xml.etree.ElementTree as ET
from dataclasses import dataclass
from pathlib import Path
from typing import Optional
import tkinter as tk
from tkinter import ttk, filedialog, messagebox

APP_DATA_DIR = Path(os.environ.get('APPDATA', Path.home() / '.config')) / 'PowerSimBuilder'
SETTINGS_FILE = APP_DATA_DIR / 'fmu_browser_settings.json'

# 互換プリセットのみ（all は削除：切り捨て問題 + 非互換 FMU 混入のため）
PRESET_CATALOGS = [
    ('FMI Cross Check (2.0 / CoSim / win64)', 'github:modelica/fmi-cross-check|2.0|cs|win64'),
    ('FMI Cross Check (3.0 / CoSim / win64)', 'github:modelica/fmi-cross-check|3.0|cs|win64'),
    ('FMI Cross Check (2.0 / CoSim / linux64)', 'github:modelica/fmi-cross-check|2.0|cs|linux64'),
    ('Custom JSON URL...', 'custom:'),
]

@dataclass
class Settings:
    github_api_key: str = ''
    output_dir: str = 'user_physics_models'
    last_catalog: str = PRESET_CATALOGS[0][1]

    @classmethod
    def load(cls):
        if SETTINGS_FILE.exists():
            try:
                d = json.loads(SETTINGS_FILE.read_text(encoding='utf-8'))
                return cls(d.get('github_api_key',''), d.get('output_dir','user_physics_models'),
                           d.get('last_catalog', PRESET_CATALOGS[0][1]))
            except Exception:
                pass
        return cls()

    def save(self):
        APP_DATA_DIR.mkdir(parents=True, exist_ok=True)
        SETTINGS_FILE.write_text(json.dumps({
            'github_api_key': self.github_api_key,
            'output_dir': self.output_dir,
            'last_catalog': self.last_catalog,
        }, indent=2), encoding='utf-8')

@dataclass
class FmuModel:
    name: str
    download_url: str
    fmi_version: str = ''
    fmi_type: str = ''
    platform: str = ''
    size_bytes: int = 0
    source: str = ''

class GitHubCatalogClient:
    """Browse FMU directories using the Contents API (no truncation)."""

    def __init__(self, api_key=''):
        self.api_key = api_key
        self._branch = None

    def _api_get(self, url):
        req = urllib.request.Request(url)
        req.add_header('Accept', 'application/vnd.github.v3+json')
        req.add_header('User-Agent', 'PowerSimBuilder-FMU-Browser')
        if self.api_key:
            req.add_header('Authorization', f'token {self.api_key}')
        try:
            with urllib.request.urlopen(req, timeout=60) as resp:
                return json.loads(resp.read().decode('utf-8'))
        except urllib.error.HTTPError as e:
            if e.code == 403:
                raise RuntimeError('GitHub API rate limit exceeded. Set an API key (github.com/settings/tokens).')
            raise RuntimeError(f'GitHub API error: {e.code} {e.reason}')
        except urllib.error.URLError as e:
            raise RuntimeError(f'Network error: {e.reason}')

    def _get_branch(self, repo):
        if self._branch:
            return self._branch
        data = self._api_get(f'https://api.github.com/repos/{repo}')
        self._branch = data.get('default_branch', 'main')
        return self._branch

    def _contents(self, repo, path):
        branch = self._get_branch(repo)
        return self._api_get(f'https://api.github.com/repos/{repo}/contents/{path}?ref={branch}')

    def fetch(self, repo, version_filter, type_filter, platform_filter, log_fn=print):
        """Browse fmus/{ver}/{type}/{platform}/ and list all models."""
        if not (version_filter and type_filter and platform_filter):
            raise RuntimeError('Filters required (version/type/platform)')

        base_path = f'fmus/{version_filter}/{type_filter}/{platform_filter}'
        branch = self._get_branch(repo)

        log_fn(f'Fetching: {repo} / {base_path} (branch: {branch})')

        # Step 1: Get tool directories
        try:
            tools = self._contents(repo, base_path)
        except RuntimeError as e:
            if '404' in str(e):
                raise RuntimeError(f'Path not found: {base_path}\nCheck if the repo has this FMI version/type/platform combination.')
            raise

        tool_dirs = [t for t in tools if t.get('type') == 'dir']
        log_fn(f'Found {len(tool_dirs)} tool directories')

        models = []
        for i, tool in enumerate(tool_dirs, 1):
            tool_name = tool['name']
            log_fn(f'  [{i}/{len(tool_dirs)}] {tool_name}...')
            try:
                model_dirs = self._contents(repo, f'{base_path}/{tool_name}')
            except Exception as e:
                log_fn(f'    [WARN] {e}')
                continue

            for md in model_dirs:
                if md.get('type') != 'dir':
                    continue
                model_name = md['name']
                fmu_path = f'{base_path}/{tool_name}/{model_name}/files/model.fmu'
                download_url = f'https://raw.githubusercontent.com/{repo}/{branch}/{fmu_path}'
                models.append(FmuModel(
                    name=model_name,
                    download_url=download_url,
                    fmi_version=version_filter,
                    fmi_type='CoSimulation' if type_filter == 'cs' else 'ModelExchange',
                    platform=platform_filter,
                    size_bytes=0,
                    source=f'{repo.split("/")[-1]} / {tool_name}',
                ))

        # Deduplicate
        seen = set()
        unique = []
        for m in models:
            key = f'{m.name}_{m.fmi_version}_{m.fmi_type}_{m.platform}'
            if key not in seen:
                seen.add(key)
                unique.append(m)

        return unique

class JsonCatalogClient:
    def fetch(self, url):
        req = urllib.request.Request(url)
        req.add_header('User-Agent', 'PowerSimBuilder-FMU-Browser')
        try:
            with urllib.request.urlopen(req, timeout=30) as resp:
                data = json.loads(resp.read().decode('utf-8'))
        except urllib.error.URLError as e:
            raise RuntimeError(f'Network error: {e.reason}')
        except json.JSONDecodeError as e:
            raise RuntimeError(f'Invalid JSON: {e}')
        models = []
        for item in data.get('models', []):
            models.append(FmuModel(
                name=item.get('name','unknown'),
                download_url=item.get('fmu_url', item.get('url','')),
                fmi_version=item.get('fmi_version',''),
                fmi_type=item.get('fmi_type',''),
                platform=item.get('platform',''),
                size_bytes=item.get('size_bytes',0),
                source=url,
            ))
        return models

def parse_fmu_metadata(fmu_path):
    with zipfile.ZipFile(fmu_path, 'r') as zf:
        with zf.open('modelDescription.xml') as f:
            tree = ET.parse(f)
            root = tree.getroot()
    fmi_version = root.get('fmiVersion','')
    model_name = root.get('modelName', fmu_path.stem)
    cs = root.find('CoSimulation')
    me = root.find('ModelExchange')
    fmi_type = 'CoSimulation' if cs is not None else ('ModelExchange' if me is not None else 'Unknown')
    inputs, outputs, parameters = [], [], []
    for mv in root.findall('ModelVariables'):
        for sv in mv.findall('ScalarVariable'):
            name = sv.get('name','')
            causality = sv.get('causality','')
            var_type = ''
            for child in sv:
                var_type = child.tag
                break
            if causality == 'input': inputs.append(name)
            elif causality == 'output': outputs.append(name)
            elif causality in ('parameter','calculatedParameter'):
                default = ''
                for child in sv:
                    default = child.get('start','')
                    break
                parameters.append({'name':name,'type':var_type,'default':default})
    return {'fmi_version':fmi_version,'model_name':model_name,'fmi_type':fmi_type,
            'inputs':inputs,'outputs':outputs,'parameters':parameters}

def generate_model_yaml(meta, fmu_relative_path):
    lines = [f'name: {meta["model_name"]}', f'fmi_version: "{meta["fmi_version"]}"',
             f'fmi_type: {meta["fmi_type"]}', f'fmu_file: {fmu_relative_path}', '', 'interface:']
    if meta['inputs']:
        lines.append('  inputs:')
        for inp in meta['inputs']:
            lines.append(f'    {inp}: {inp}')
    else:
        lines.append('  inputs: {}')
    if meta['outputs']:
        lines.append('  outputs:')
        for out in meta['outputs']:
            lines.append(f'    {out}: {out}')
    else:
        lines.append('  outputs: {}')
    if meta['parameters']:
        lines.append('')
        lines.append('parameters:')
        for p in meta['parameters']:
            default = p['default'] if p['default'] else '0'
            lines.append(f'  {p["name"]}:')
            lines.append(f'    type: {p["type"]}')
            lines.append(f'    default: {default}')
    lines.append('')
    return '\n'.join(lines)

def download_fmu(model, output_dir, log_fn):
    try:
        model_dir = output_dir / model.name
        source_dir = model_dir / 'source'
        source_dir.mkdir(parents=True, exist_ok=True)
        fmu_path = source_dir / f'{model.name}.fmu'
        log_fn(f'Downloading {model.name}...')
        req = urllib.request.Request(model.download_url)
        req.add_header('User-Agent', 'PowerSimBuilder-FMU-Browser')
        with urllib.request.urlopen(req, timeout=120) as resp:
            fmu_path.write_bytes(resp.read())
        size_kb = fmu_path.stat().st_size / 1024
        log_fn(f'  Downloaded: {size_kb:.0f} KB')
        log_fn(f'  Parsing modelDescription.xml...')
        meta = parse_fmu_metadata(fmu_path)
        model.fmi_version = meta['fmi_version']
        model.fmi_type = meta['fmi_type']
        yaml_path = model_dir / 'model.yaml'
        yaml_path.write_text(generate_model_yaml(meta, f'source/{model.name}.fmu'), encoding='utf-8')
        log_fn(f'  model.yaml: {len(meta["inputs"])} in, {len(meta["outputs"])} out, {len(meta["parameters"])} params')
        log_fn(f'  Installed to: {model_dir}')
        if meta['fmi_type'] != 'CoSimulation':
            log_fn(f'  [WARN] {meta["fmi_type"]} — not CoSimulation')
        return True
    except Exception as e:
        log_fn(f'  [ERROR] {e}')
        return False

class FMUCatalogBrowser(tk.Tk):
    def __init__(self):
        super().__init__()
        self.title('FMU Catalog Browser v3')
        self.geometry('900x700')
        self.minsize(700, 500)
        self.settings = Settings.load()
        self.models = []
        self._build_ui()
        self._restore_settings()

    def _build_ui(self):
        top = ttk.Frame(self, padding=8)
        top.pack(fill='x')
        ttk.Label(top, text='GitHub API Key:').grid(row=0, column=0, sticky='w')
        self.api_key_var = tk.StringVar()
        self.api_key_entry = ttk.Entry(top, textvariable=self.api_key_var, width=50, show='*')
        self.api_key_entry.grid(row=0, column=1, sticky='ew', padx=4)
        self.show_key_var = tk.BooleanVar()
        ttk.Checkbutton(top, text='Show', variable=self.show_key_var,
                        command=self._toggle_key).grid(row=0, column=2)
        ttk.Button(top, text='Save', command=self._save_settings).grid(row=0, column=3, padx=4)
        ttk.Label(top, text='(optional, 60→5000 req/hour)', foreground='gray').grid(row=1, column=1, columnspan=3, sticky='w')

        cat_frame = ttk.LabelFrame(self, text='Catalog Source', padding=8)
        cat_frame.pack(fill='x', padx=8, pady=4)
        ttk.Label(cat_frame, text='Source:').grid(row=0, column=0, sticky='w')
        self.catalog_var = tk.StringVar()
        self.catalog_combo = ttk.Combobox(cat_frame, textvariable=self.catalog_var,
                                          values=[n for n,_ in PRESET_CATALOGS], state='readonly', width=45)
        self.catalog_combo.grid(row=0, column=1, sticky='ew', padx=4)
        self.catalog_combo.current(0)
        self.catalog_combo.bind('<<ComboboxSelected>>', self._on_cat_change)
        self.custom_url_var = tk.StringVar()
        self.custom_url_entry = ttk.Entry(cat_frame, textvariable=self.custom_url_var, width=50, state='disabled')
        self.custom_url_entry.grid(row=1, column=1, sticky='ew', padx=4, pady=(4,0))
        ttk.Button(cat_frame, text='Fetch Catalog', command=self._fetch).grid(row=0, column=2, padx=4)

        sf = ttk.Frame(self, padding=(8,4))
        sf.pack(fill='x')
        ttk.Label(sf, text='Search:').pack(side='left')
        self.search_var = tk.StringVar()
        self.search_var.trace('w', self._on_search)
        ttk.Entry(sf, textvariable=self.search_var, width=30).pack(side='left', padx=4)

        lf = ttk.LabelFrame(self, text='Available FMU Models (CoSimulation only)', padding=4)
        lf.pack(fill='both', expand=True, padx=8, pady=4)
        cols = ('select','name','fmi_version','fmi_type','platform','size','source')
        self.tree = ttk.Treeview(lf, columns=cols, show='headings', height=15)
        for c,l,w,a in [('select','✓',30,'center'),('name','Name',200,'w'),
                        ('fmi_version','FMI',50,'center'),('fmi_type','Type',100,'w'),
                        ('platform','Platform',70,'center'),('size','Size',70,'e'),('source','Source',200,'w')]:
            self.tree.heading(c, text=l)
            self.tree.column(c, width=w, anchor=a)
        self.tree.pack(side='left', fill='both', expand=True)
        sb = ttk.Scrollbar(lf, orient='vertical', command=self.tree.yview)
        self.tree.configure(yscrollcommand=sb.set)
        sb.pack(side='right', fill='y')
        self.tree.bind('<Button-1>', self._on_click)

        bf = ttk.Frame(self, padding=(8,4))
        bf.pack(fill='x')
        ttk.Button(bf, text='Select All', command=self._sel_all).pack(side='left', padx=2)
        ttk.Button(bf, text='Deselect All', command=self._desel_all).pack(side='left', padx=2)
        ttk.Label(bf, text='Output Dir:').pack(side='left', padx=(16,4))
        self.out_var = tk.StringVar()
        ttk.Entry(bf, textvariable=self.out_var, width=30).pack(side='left', padx=2)
        ttk.Button(bf, text='Browse...', command=self._browse).pack(side='left', padx=2)
        self.dl_btn = ttk.Button(bf, text='Download Selected', command=self._download)
        self.dl_btn.pack(side='right', padx=2)

        logf = ttk.LabelFrame(self, text='Log', padding=4)
        logf.pack(fill='both', expand=False, padx=8, pady=(4,8))
        self.log_text = tk.Text(logf, height=8, wrap='word', state='disabled')
        self.log_text.pack(side='left', fill='both', expand=True)
        ls = ttk.Scrollbar(logf, orient='vertical', command=self.log_text.yview)
        self.log_text.configure(yscrollcommand=ls.set)
        ls.pack(side='right', fill='y')

        self.status_var = tk.StringVar(value='Ready')
        ttk.Label(self, textvariable=self.status_var, relief='sunken', anchor='w').pack(fill='x', side='bottom')

    def _restore_settings(self):
        self.api_key_var.set(self.settings.github_api_key)
        self.out_var.set(self.settings.output_dir)
        for i,(n,v) in enumerate(PRESET_CATALOGS):
            if v == self.settings.last_catalog:
                self.catalog_combo.current(i)
                self._on_cat_change()
                break

    def _toggle_key(self):
        self.api_key_entry.config(show='' if self.show_key_var.get() else '*')

    def _save_settings(self):
        self.settings.github_api_key = self.api_key_var.get().strip()
        self.settings.output_dir = self.out_var.get().strip()
        self.settings.save()
        self._log(f'Settings saved')
        self.status_var.set('Settings saved')

    def _on_cat_change(self, e=None):
        idx = self.catalog_combo.current()
        if idx >= 0:
            v = PRESET_CATALOGS[idx][1]
            self.custom_url_entry.config(state='normal' if v.startswith('custom:') else 'disabled')

    def _get_source(self):
        idx = self.catalog_combo.current()
        if idx >= 0:
            v = PRESET_CATALOGS[idx][1]
            if v.startswith('custom:'):
                url = self.custom_url_var.get().strip()
                return f'custom:{url}' if url else ''
            return v
        return ''

    def _fetch(self):
        source = self._get_source()
        if not source:
            messagebox.showwarning('No Source', 'Select a catalog source.')
            return
        api_key = self.api_key_var.get().strip()
        self.settings.github_api_key = api_key
        self.settings.last_catalog = source
        self.settings.save()
        self.status_var.set('Fetching...')
        self._log(f'Fetching: {source}')

        def worker():
            try:
                if source.startswith('github:'):
                    parts = source[7:].split('|')
                    repo, ver, typ, plat = parts[0], parts[1], parts[2], parts[3]
                    client = GitHubCatalogClient(api_key=api_key)
                    models = client.fetch(repo, ver, typ, plat, log_fn=lambda m: self.after(0, lambda m=m: self._log(m)))
                elif source.startswith('custom:'):
                    models = JsonCatalogClient().fetch(source[7:])
                else:
                    raise RuntimeError(f'Unknown: {source}')
                self.models = models
                self.after(0, lambda: self._populate(models))
                self.after(0, lambda: self._log(f'Found {len(models)} FMU model(s)'))
                self.after(0, lambda: self.status_var.set(f'Found {len(models)} models'))
            except Exception as e:
                self.after(0, lambda: self._log(f'[ERROR] {e}'))
                self.after(0, lambda: self.status_var.set('Failed'))
                self.after(0, lambda: messagebox.showerror('Failed', str(e)))

        threading.Thread(target=worker, daemon=True).start()

    def _populate(self, models):
        for item in self.tree.get_children():
            self.tree.delete(item)
        for m in models:
            sz = f'{m.size_bytes/1024:.0f} KB' if m.size_bytes else '-'
            self.tree.insert('', 'end', values=('☐', m.name, m.fmi_version or '?',
                                                m.fmi_type or '?', m.platform or '?', sz, m.source))

    def _on_search(self, *a):
        q = self.search_var.get().lower()
        for item in self.tree.get_children():
            vals = self.tree.item(item, 'values')
            name = vals[1].lower() if len(vals) > 1 else ''
            if q and q not in name:
                self.tree.detach(item)
            else:
                self.tree.move(item, '', 'end')

    def _on_click(self, event):
        if self.tree.identify_column(event.x) == '#1':
            item = self.tree.identify_row(event.y)
            if item:
                vals = list(self.tree.item(item, 'values'))
                vals[0] = '☑' if vals[0] == '☐' else '☐'
                self.tree.item(item, values=vals)

    def _sel_all(self):
        for item in self.tree.get_children():
            vals = list(self.tree.item(item, 'values'))
            vals[0] = '☑'
            self.tree.item(item, values=vals)

    def _desel_all(self):
        for item in self.tree.get_children():
            vals = list(self.tree.item(item, 'values'))
            vals[0] = '☐'
            self.tree.item(item, values=vals)

    def _browse(self):
        d = filedialog.askdirectory(initialdir=self.out_var.get() or '.')
        if d:
            self.out_var.set(d)

    def _get_selected(self):
        sel = []
        for item in self.tree.get_children():
            vals = self.tree.item(item, 'values')
            if vals[0] == '☑':
                for m in self.models:
                    if m.name == vals[1]:
                        sel.append(m)
                        break
        return sel

    def _download(self):
        sel = self._get_selected()
        if not sel:
            messagebox.showinfo('No Selection', 'Select at least one model.')
            return
        out = Path(self.out_var.get().strip() or 'user_physics_models')
        if not out.is_absolute():
            out = Path(__file__).resolve().parent.parent / out
        self.dl_btn.config(state='disabled')
        self.status_var.set(f'Downloading {len(sel)} model(s)...')

        def worker():
            ok, fail = 0, 0
            for i, model in enumerate(sel, 1):
                self.after(0, lambda i=i, n=len(sel): self.status_var.set(f'{i}/{n}...'))
                r = download_fmu(model, out, lambda m: self.after(0, lambda m=m: self._log(m)))
                if r: ok += 1
                else: fail += 1
            self.after(0, lambda: self._log(f'\nDone: {ok} OK, {fail} failed.'))
            self.after(0, lambda: self.status_var.set(f'Done: {ok} OK, {fail} failed'))
            self.after(0, lambda: self.dl_btn.config(state='normal'))
            if fail == 0:
                self.after(0, lambda: messagebox.showinfo('Complete', f'{ok} FMU(s) installed to:\n{out}'))
            else:
                self.after(0, lambda: messagebox.showwarning('Partial', f'{ok} OK, {fail} failed.'))

        threading.Thread(target=worker, daemon=True).start()

    def _log(self, msg):
        self.log_text.config(state='normal')
        self.log_text.insert('end', msg + '\n')
        self.log_text.see('end')
        self.log_text.config(state='disabled')
        self.update_idletasks()

def main():
    FMUCatalogBrowser().mainloop()

if __name__ == '__main__':
    main()
