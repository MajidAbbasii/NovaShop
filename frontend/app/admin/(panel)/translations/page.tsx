'use client';

import * as React from 'react';
import { useLocale } from '@/lib/locale-context';
import {
  getTranslations,
  getMissingTranslations,
  createTranslation,
  updateTranslation,
  deleteTranslation,
  type TranslationRow,
  type MissingReport,
} from '@/lib/admin-api';
import { Button } from '@/components/ui/button';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Textarea } from '@/components/ui/textarea';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogFooter,
  DialogClose,
} from '@/components/ui/dialog';
import {
  Select,
  SelectContent,
  SelectItem,
  SelectTrigger,
  SelectValue,
} from '@/components/ui/select';

const LOCALES = ['fa', 'en', 'ar'] as const;

export default function AdminTranslationsPage() {
  const { t, tva } = useLocale();
  const [rows, setRows] = React.useState<TranslationRow[]>([]);
  const [total, setTotal] = React.useState(0);
  const [page, setPage] = React.useState(1);
  const [pageSize] = React.useState(20);
  const [search, setSearch] = React.useState('');
  const [locale, setLocale] = React.useState<string>('');
  const [namespace, setNamespace] = React.useState<string>('');
  const [onlyMissing, setOnlyMissing] = React.useState(false);
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);
  const [tab, setTab] = React.useState<'list' | 'missing'>('list');

  const [missing, setMissing] = React.useState<MissingReport | null>(null);

  // editor
  const [editing, setEditing] = React.useState<TranslationRow | null>(null);
  const [editorOpen, setEditorOpen] = React.useState(false);
  const [delTarget, setDelTarget] = React.useState<TranslationRow | null>(null);

  const load = React.useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await getTranslations({
        pageNumber: page,
        pageSize,
        search: search || undefined,
        locale: locale || undefined,
        namespace: namespace || undefined,
        onlyMissing,
      });
      setRows(data.items);
      setTotal(data.totalCount);
    } catch (e: unknown) {
      setError((e as { message?: string }).message ?? 'load failed');
    } finally {
      setLoading(false);
    }
  }, [page, pageSize, search, locale, namespace, onlyMissing]);

  React.useEffect(() => {
    if (tab === 'list') void load();
  }, [tab, load]);

  const loadMissing = React.useCallback(async () => {
    setLoading(true);
    try {
      setMissing(await getMissingTranslations());
    } catch (e: unknown) {
      setError((e as { message?: string }).message ?? 'load failed');
    } finally {
      setLoading(false);
    }
  }, []);

  // Edit modal: load all locales for the key, group them.
  const openEditor = React.useCallback(async (row: TranslationRow) => {
    setEditing(row);
    setEditorOpen(true);
  }, []);

  const onDelete = async () => {
    if (!delTarget) return;
    try {
      await deleteTranslation(delTarget.id);
      setDelTarget(null);
      await load();
    } catch (e: unknown) {
      setError((e as { message?: string }).message ?? 'delete failed');
    }
  };

  const totalPages = Math.max(1, Math.ceil(total / pageSize));

  return (
    <div dir={undefined} className="space-y-4">
      <div className="flex flex-wrap items-center justify-between gap-2">
        <h1 className="text-2xl font-bold">{t('admin.translations.title')}</h1>
        <div className="flex gap-2">
          <Button variant={tab === 'list' ? 'default' : 'outline'} onClick={() => { setTab('list'); }}>
            {t('admin.translations.title')}
          </Button>
          <Button variant={tab === 'missing' ? 'default' : 'outline'} onClick={() => { setTab('missing'); void loadMissing(); }}>
            Missing ({missing?.missingCount ?? '…'})
          </Button>
        </div>
      </div>

      {error && (
        <div className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">{error}</div>
      )}

      {tab === 'list' ? (
        <>
          <Card>
            <CardContent className="flex flex-wrap items-end gap-3 pt-6">
              <div className="flex flex-col gap-1">
                <Label>{t('admin.products.search')}</Label>
                <Input
                  value={search}
                  onChange={(e) => setSearch(e.target.value)}
                  placeholder="common.save"
                  className="w-56"
                />
              </div>
              <div className="flex flex-col gap-1">
                <Label>Locale</Label>
                <Select value={locale || 'all'} onValueChange={(v) => setLocale(v === 'all' ? '' : v)}>
                  <SelectTrigger className="w-32"><SelectValue /></SelectTrigger>
                  <SelectContent>
                    <SelectItem value="all">All</SelectItem>
                    {LOCALES.map((l) => (
                      <SelectItem key={l} value={l}>{l}</SelectItem>
                    ))}
                  </SelectContent>
                </Select>
              </div>
              <div className="flex flex-col gap-1">
                <Label>Namespace</Label>
                <Input
                  value={namespace}
                  onChange={(e) => setNamespace(e.target.value)}
                  placeholder="admin"
                  className="w-32"
                />
              </div>
              <Button onClick={() => { setPage(1); void load(); }}>{t('common.search')}</Button>
              <Button
                variant="outline"
                onClick={() => { setSearch(''); setLocale(''); setNamespace(''); setOnlyMissing(false); setPage(1); void load(); }}
              >
                {t('category.clear')}
              </Button>
              <Button onClick={() => { setEditing(null); setEditorOpen(true); }}>
                {t('common.create')}
              </Button>
            </CardContent>
          </Card>

          <Card>
            <CardContent className="pt-6">
              <Table>
                <TableHeader>
                  <TableRow>
                    <TableHead>Key</TableHead>
                    <TableHead>Locale</TableHead>
                    <TableHead>Value</TableHead>
                    <TableHead>Namespace</TableHead>
                    <TableHead className="text-end">{t('common.actions')}</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {rows.map((r) => (
                    <TableRow key={r.id}>
                      <TableCell className="font-mono text-xs">{r.key}</TableCell>
                      <TableCell>{r.locale}</TableCell>
                      <TableCell className="max-w-[320px] truncate">{r.value}</TableCell>
                      <TableCell>{r.namespace ?? '-'}</TableCell>
                      <TableCell className="text-end">
                        <div className="flex justify-end gap-1">
                          <Button size="sm" variant="outline" onClick={() => openEditor(r)}>{t('common.edit')}</Button>
                          <Button size="sm" variant="destructive" onClick={() => setDelTarget(r)}>{t('common.delete')}</Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                  {!loading && rows.length === 0 && (
                    <TableRow><TableCell colSpan={5} className="text-center text-muted-foreground py-8">No translations found</TableCell></TableRow>
                  )}
                </TableBody>
              </Table>

              <div className="mt-4 flex items-center justify-between">
                <span className="text-sm text-muted-foreground">
                  {tva('pagination.showing', { from: (page - 1) * pageSize + 1, to: Math.min(page * pageSize, total), total })}
                </span>
                <div className="flex gap-2">
                  <Button size="sm" variant="outline" disabled={page <= 1} onClick={() => setPage((p) => p - 1)}>{t('pagination.previous')}</Button>
                  <span className="px-2 py-1 text-sm">{page} / {totalPages}</span>
                  <Button size="sm" variant="outline" disabled={page >= totalPages} onClick={() => setPage((p) => p + 1)}>{t('pagination.next')}</Button>
                </div>
              </div>
            </CardContent>
          </Card>
        </>
      ) : (
        <Card>
          <CardHeader><CardTitle>Missing Translations ({missing?.missingCount ?? 0})</CardTitle></CardHeader>
          <CardContent>
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Key</TableHead>
                  <TableHead>Missing in</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {missing?.missing.map((m) => (
                  <TableRow key={m.key}>
                    <TableCell className="font-mono text-xs">{m.key}</TableCell>
                    <TableCell>{m.missingLocales.join(', ')}</TableCell>
                  </TableRow>
                ))}
                {!loading && missing && missing.missing.length === 0 && (
                  <TableRow><TableCell colSpan={2} className="text-center text-muted-foreground py-8">All keys complete</TableCell></TableRow>
                )}
              </TableBody>
            </Table>
          </CardContent>
        </Card>
      )}

      <TranslationEditor
        open={editorOpen}
        row={editing}
        onClose={() => setEditorOpen(false)}
        onSaved={() => { setEditorOpen(false); void load(); }}
      />

      <Dialog open={!!delTarget} onOpenChange={(o) => !o && setDelTarget(null)}>
        <DialogContent>
          <DialogHeader>
            <DialogTitle>{t('common.delete')}</DialogTitle>
          </DialogHeader>
          <p className="text-sm text-muted-foreground">
            {tva('admin.products.deleteConfirm', { name: delTarget ? `${delTarget.key} (${delTarget.locale})` : '' })}
          </p>
          <DialogFooter>
            <DialogClose asChild><Button variant="outline">{t('common.cancel')}</Button></DialogClose>
            <Button variant="destructive" onClick={onDelete}>{t('common.delete')}</Button>
          </DialogFooter>
        </DialogContent>
      </Dialog>
    </div>
  );
}

function TranslationEditor({
  open,
  row,
  onClose,
  onSaved,
}: {
  open: boolean;
  row: TranslationRow | null;
  onClose: () => void;
  onSaved: () => void;
}) {
  const { t } = useLocale();
  const isNew = !row;
  const [key, setKey] = React.useState('');
  const [namespace, setNamespace] = React.useState('');
  const [description, setDescription] = React.useState('');
  const [values, setValues] = React.useState<Record<string, string>>({ fa: '', en: '', ar: '' });
  const [saving, setSaving] = React.useState(false);
  const [error, setError] = React.useState<string | null>(null);

  React.useEffect(() => {
    if (!open) return;
    setError(null);
    setSaving(false);
    if (row) {
      // editing one row: prefill that locale; other locales left to be fetched if needed.
      setKey(row.key);
      setNamespace(row.namespace ?? '');
      setDescription(row.description ?? '');
      setValues({ fa: row.locale === 'fa' ? row.value : '', en: row.locale === 'en' ? row.value : '', ar: row.locale === 'ar' ? row.value : '' });
    } else {
      setKey('');
      setNamespace('');
      setDescription('');
      setValues({ fa: '', en: '', ar: '' });
    }
  }, [open, row]);

  const onSave = async () => {
    setSaving(true);
    setError(null);
    try {
      if (isNew) {
        const vals = LOCALES.filter((l) => values[l]?.trim()).map((l) => ({ locale: l, value: values[l] }));
        if (!key.trim()) throw new Error('Key is required');
        if (vals.length === 0) throw new Error('At least one translation is required');
        await createTranslation({ key: key.trim(), namespace: namespace.trim() || undefined, description: description.trim() || undefined, values: vals });
      } else if (row) {
        // update this single row's value
        await updateTranslation(row.id, { value: values[row.locale] ?? '', namespace: namespace.trim() || undefined, description: description.trim() || undefined });
      }
      onSaved();
    } catch (e: unknown) {
      setError((e as { message?: string }).message ?? 'save failed');
    } finally {
      setSaving(false);
    }
  };

  return (
    <Dialog open={open} onOpenChange={(o) => !o && onClose()}>
      <DialogContent className="max-w-lg">
        <DialogHeader>
          <DialogTitle>{isNew ? t('common.create') : t('common.edit')}</DialogTitle>
        </DialogHeader>
        <div className="space-y-3">
          <div className="flex flex-col gap-1">
            <Label>Key</Label>
            <Input value={key} disabled={!isNew} onChange={(e) => setKey(e.target.value)} placeholder="common.save" className="font-mono" />
          </div>
          <div className="flex flex-col gap-1">
            <Label>Namespace</Label>
            <Input value={namespace} onChange={(e) => setNamespace(e.target.value)} placeholder="common" />
          </div>
          <div className="flex flex-col gap-1">
            <Label>Description</Label>
            <Textarea value={description} onChange={(e) => setDescription(e.target.value)} placeholder="Where this key is used" />
          </div>
          {LOCALES.map((l) => (
            <div className="flex flex-col gap-1" key={l}>
              <Label className="uppercase">{l}</Label>
              <Input dir={l === 'fa' || l === 'ar' ? 'rtl' : 'ltr'} value={values[l]} onChange={(e) => setValues((v) => ({ ...v, [l]: e.target.value }))} />
            </div>
          ))}
          {error && <div className="rounded-md bg-destructive/10 px-3 py-2 text-sm text-destructive">{error}</div>}
        </div>
        <DialogFooter>
          <DialogClose asChild><Button variant="outline">{t('common.cancel')}</Button></DialogClose>
          <Button onClick={onSave} disabled={saving}>{saving ? t('common.loading') : t('common.save')}</Button>
        </DialogFooter>
      </DialogContent>
    </Dialog>
  );
}
